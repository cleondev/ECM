using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Ecm.FileIntegration;

public sealed class EcmFileClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<EcmFileClient> _logger;
    private readonly EcmIntegrationOptions _options;
    private readonly EcmOnBehalfAuthenticator _onBehalfAuthenticator;

    public EcmFileClient(
        HttpClient httpClient,
        ILogger<EcmFileClient> logger,
        IOptions<EcmIntegrationOptions> options,
        EcmOnBehalfAuthenticator onBehalfAuthenticator)
    {
        _httpClient = httpClient;
        _logger = logger;
        _options = options.Value;
        _onBehalfAuthenticator = onBehalfAuthenticator;
    }

    public async Task<UserProfile?> GetCurrentUserProfileAsync(CancellationToken cancellationToken)
    {
        await _onBehalfAuthenticator.EnsureSignedInAsync(_httpClient, cancellationToken);

        using var response = await _httpClient.GetAsync("/api/iam/profile", cancellationToken);
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            _logger.LogError("Request to {Url} was unauthorized. Ensure AccessToken is configured.", response.RequestMessage?.RequestUri);
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<UserProfile>(cancellationToken: cancellationToken);
    }

    public async Task<DocumentDto?> UploadDocumentAsync(DocumentUploadRequest uploadRequest, CancellationToken cancellationToken)
    {
        await using var stream = File.OpenRead(uploadRequest.FilePath);

        using var content = new MultipartFormDataContent();
        content.Add(new StringContent(uploadRequest.OwnerId.ToString()), "ownerId");
        content.Add(new StringContent(uploadRequest.CreatedBy.ToString()), "createdBy");
        content.Add(new StringContent(uploadRequest.DocType), "docType");
        content.Add(new StringContent(uploadRequest.Status), "status");
        content.Add(new StringContent(uploadRequest.Sensitivity), "sensitivity");

        if (uploadRequest.DocumentTypeId is { } docTypeId)
        {
            content.Add(new StringContent(docTypeId.ToString()), "documentTypeId");
        }

        if (!string.IsNullOrWhiteSpace(uploadRequest.Title))
        {
            content.Add(new StringContent(uploadRequest.Title), "title");
        }

        var fileContent = new StreamContent(stream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(uploadRequest.ContentType ?? "application/octet-stream");
        content.Add(fileContent, "file", Path.GetFileName(uploadRequest.FilePath));

        await _onBehalfAuthenticator.EnsureSignedInAsync(_httpClient, cancellationToken);

        using var response = await _httpClient.PostAsync(GetDocumentsEndpoint(), content, cancellationToken);

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "Failed to upload document. Status: {StatusCode}. Body: {Body}",
                response.StatusCode,
                body);
            throw new HttpRequestException($"Upload failed with status {(int)response.StatusCode}: {body}");
        }

        return JsonSerializer.Deserialize<DocumentDto>(body, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
        });
    }

    public async Task<Uri?> GetDownloadUriAsync(Guid versionId, CancellationToken cancellationToken)
    {
        await _onBehalfAuthenticator.EnsureSignedInAsync(_httpClient, cancellationToken);

        using var request = new HttpRequestMessage(HttpMethod.Get, $"{GetDownloadEndpoint()}/{versionId}");
        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            _logger.LogWarning("Version {VersionId} not found when requesting download URL.", versionId);
            return null;
        }

        var location = response.Headers.Location;
        if (IsRedirect(response.StatusCode) && location is not null)
        {
            return location.IsAbsoluteUri ? location : new Uri(_httpClient.BaseAddress!, location);
        }

        if (response.IsSuccessStatusCode)
        {
            if (location is not null && !location.IsAbsoluteUri && _httpClient.BaseAddress is not null)
            {
                location = new Uri(_httpClient.BaseAddress, location);
            }

            return location;
        }

        _logger.LogError(
            "Unexpected status {StatusCode} while requesting download URL for {VersionId}.",
            response.StatusCode,
            versionId);
        return null;
    }

    public async Task<IReadOnlyCollection<TagLabelDto>> ListTagsAsync(CancellationToken cancellationToken)
    {
        await _onBehalfAuthenticator.EnsureSignedInAsync(_httpClient, cancellationToken);

        using var response = await _httpClient.GetAsync(GetTagsEndpoint(), cancellationToken);
        response.EnsureSuccessStatusCode();

        var tags = await response.Content.ReadFromJsonAsync<TagLabelDto[]>(cancellationToken: cancellationToken);
        return tags ?? Array.Empty<TagLabelDto>();
    }

    public async Task<TagLabelDto?> CreateTagAsync(TagCreateRequest request, CancellationToken cancellationToken)
    {
        await _onBehalfAuthenticator.EnsureSignedInAsync(_httpClient, cancellationToken);

        using var response = await _httpClient.PostAsJsonAsync(GetTagsEndpoint(), request, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<TagLabelDto>(cancellationToken: cancellationToken);
    }

    public async Task<TagLabelDto?> UpdateTagAsync(Guid tagId, TagUpdateRequest request, CancellationToken cancellationToken)
    {
        await _onBehalfAuthenticator.EnsureSignedInAsync(_httpClient, cancellationToken);

        using var response = await _httpClient.PutAsJsonAsync($"{GetTagsEndpoint()}/{tagId}", request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            _logger.LogWarning("Không tìm thấy tag {TagId} khi cập nhật.", tagId);
            return null;
        }

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<TagLabelDto>(cancellationToken: cancellationToken);
    }

    public async Task<bool> DeleteTagAsync(Guid tagId, CancellationToken cancellationToken)
    {
        await _onBehalfAuthenticator.EnsureSignedInAsync(_httpClient, cancellationToken);

        using var response = await _httpClient.DeleteAsync($"{GetTagsEndpoint()}/{tagId}", cancellationToken);

        if (response.StatusCode is HttpStatusCode.NotFound)
        {
            _logger.LogWarning("Không tìm thấy tag {TagId} khi xoá.", tagId);
            return false;
        }

        response.EnsureSuccessStatusCode();
        return true;
    }

    public async Task<DocumentListResult?> ListDocumentsAsync(DocumentListQuery query, CancellationToken cancellationToken)
    {
        await _onBehalfAuthenticator.EnsureSignedInAsync(_httpClient, cancellationToken);

        var url = AppendQueryString(GetDocumentsEndpoint(), BuildDocumentQueryParameters(query));
        using var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<DocumentListResult>(cancellationToken: cancellationToken);
    }

    public async Task<DocumentListItem?> UpdateDocumentAsync(Guid documentId, DocumentUpdateRequest request, CancellationToken cancellationToken)
    {
        await _onBehalfAuthenticator.EnsureSignedInAsync(_httpClient, cancellationToken);

        using var response = await _httpClient.PutAsJsonAsync($"{GetDocumentsEndpoint()}/{documentId}", request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            _logger.LogWarning("Document {DocumentId} không tồn tại khi cập nhật.", documentId);
            return null;
        }

        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            _logger.LogWarning("Không đủ quyền cập nhật document {DocumentId}.", documentId);
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<DocumentListItem>(cancellationToken: cancellationToken);
    }

    public async Task<bool> DeleteDocumentAsync(Guid documentId, CancellationToken cancellationToken)
    {
        await _onBehalfAuthenticator.EnsureSignedInAsync(_httpClient, cancellationToken);

        using var response = await _httpClient.DeleteAsync($"{GetDocumentsEndpoint()}/{documentId}", cancellationToken);

        if (response.StatusCode is HttpStatusCode.NotFound)
        {
            _logger.LogWarning("Document {DocumentId} không tồn tại khi xoá.", documentId);
            return false;
        }

        if (response.StatusCode is HttpStatusCode.Forbidden)
        {
            _logger.LogWarning("Không đủ quyền xoá document {DocumentId}.", documentId);
            return false;
        }

        response.EnsureSuccessStatusCode();
        return true;
    }

    private string GetDocumentsEndpoint() => _options.OnBehalf.Enabled
        ? "/api/documents"
        : "/api/ecm/documents";

    private string GetDownloadEndpoint() => _options.OnBehalf.Enabled
        ? "/api/documents/files/download"
        : "/api/ecm/files/download";

    private string GetTagsEndpoint() => _options.OnBehalf.Enabled
        ? "/api/tags"
        : "/api/ecm/tags";

    private static Dictionary<string, string?> BuildDocumentQueryParameters(DocumentListQuery query)
    {
        var parameters = new Dictionary<string, string?>();

        AddIfNotEmpty(parameters, "q", query.Query);
        AddIfNotEmpty(parameters, "doc_type", query.DocType);
        AddIfNotEmpty(parameters, "status", query.Status);
        AddIfNotEmpty(parameters, "sensitivity", query.Sensitivity);

        if (query.OwnerId is { } owner)
        {
            parameters["owner_id"] = owner.ToString();
        }

        if (query.GroupId is { } groupId)
        {
            parameters["group_id"] = groupId.ToString();
        }

        parameters["page"] = query.Page.ToString(CultureInfo.InvariantCulture);
        parameters["pageSize"] = query.PageSize.ToString(CultureInfo.InvariantCulture);

        return parameters;
    }

    private static void AddIfNotEmpty(IDictionary<string, string?> parameters, string key, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            parameters[key] = value;
        }
    }

    private static string AppendQueryString(string basePath, IReadOnlyDictionary<string, string?> parameters)
    {
        var filtered = parameters
            .Where(kvp => !string.IsNullOrWhiteSpace(kvp.Value))
            .Select(kvp => $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value!)}")
            .ToArray();

        if (filtered.Length == 0)
        {
            return basePath;
        }

        return string.Concat(basePath, '?', string.Join('&', filtered));
    }

    private static bool IsRedirect(HttpStatusCode statusCode) =>
        statusCode is HttpStatusCode.Moved or HttpStatusCode.Redirect or HttpStatusCode.RedirectKeepVerb or HttpStatusCode.RedirectMethod;
}
