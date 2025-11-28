using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

using Ecm.Sdk.Configuration;
using Ecm.Sdk.Models.Documents;
using Ecm.Sdk.Models.Tags;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Ecm.Sdk.Clients;

/// <summary>
/// Provides helper methods for interacting with ECM document and tag APIs.
/// </summary>
public sealed class EcmFileClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<EcmFileClient> _logger;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="EcmFileClient"/> class.
    /// </summary>
    /// <param name="httpClient">HTTP client configured for ECM communication.</param>
    /// <param name="logger">Logger used for diagnostics.</param>
    /// <param name="options">Integration options injected via configuration.</param>
    public EcmFileClient(
        HttpClient httpClient,
        ILogger<EcmFileClient> logger,
        IOptionsSnapshot<EcmIntegrationOptions> options)
    {
        _httpClient = httpClient;
        _logger = logger;
        _httpClient.BaseAddress = new Uri(options.Value.BaseUrl, UriKind.Absolute);
        _httpClient.Timeout = TimeSpan.FromSeconds(100);
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("ecm-sdk/1.0");
    }

    /// <summary>
    /// Retrieves the profile for the current authenticated user.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>The user profile when available; otherwise, <c>null</c> for unauthorized requests.</returns>
    public async Task<UserProfile?> GetCurrentUserProfileAsync(CancellationToken cancellationToken)
    {
        using var response = await _httpClient.GetAsync("api/iam/profile", cancellationToken);
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            _logger.LogError(
                "Request to {Url} was unauthorized. Ensure API key configuration is valid.",
                response.RequestMessage?.RequestUri);
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<UserProfile>(cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Uploads a document and metadata to ECM.
    /// </summary>
    /// <param name="uploadRequest">Metadata describing the document being uploaded.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>The created document record.</returns>
    public async Task<DocumentDto?> UploadDocumentAsync(DocumentUploadRequest uploadRequest, CancellationToken cancellationToken)
    {
        using var content = new MultipartFormDataContent
        {
            { new StringContent(uploadRequest.OwnerId.ToString()), "ownerId" },
            { new StringContent(uploadRequest.CreatedBy.ToString()), "createdBy" },
            { new StringContent(uploadRequest.DocType), "docType" },
            { new StringContent(uploadRequest.Status), "status" },
            { new StringContent(uploadRequest.Sensitivity), "sensitivity" }
        };

        if (uploadRequest.DocumentTypeId is { } docTypeId)
        {
            content.Add(new StringContent(docTypeId.ToString()), "documentTypeId");
        }

        if (!string.IsNullOrWhiteSpace(uploadRequest.Title))
        {
            content.Add(new StringContent(uploadRequest.Title), "title");
        }

        var fileContent = new StreamContent(uploadRequest.FileContent);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(uploadRequest.ContentType ?? "application/octet-stream");

        var fileName = string.IsNullOrWhiteSpace(uploadRequest.FileName) ? Guid.NewGuid().ToString() : uploadRequest.FileName;
        content.Add(fileContent, "file", fileName);

        using var response = await _httpClient.PostAsync(GetDocumentsEndpoint, content, cancellationToken);

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError(
                "Failed to upload document. Status: {StatusCode}. Body: {Body}",
                response.StatusCode,
                body);
            throw new HttpRequestException($"Upload failed with status {(int)response.StatusCode}: {body}");
        }

        return JsonSerializer.Deserialize<DocumentDto>(body, _jsonOptions);
    }

    /// <summary>
    /// Uploads multiple documents and metadata to ECM in a single request.
    /// </summary>
    /// <param name="uploadRequest">Metadata describing the documents being uploaded.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>The combined batch upload result.</returns>
    public async Task<DocumentBatchResult> UploadDocumentsBatchAsync(DocumentBatchUploadRequest uploadRequest, CancellationToken cancellationToken)
    {
        var fileStreams = new List<Stream>();

        try
        {
            using var content = new MultipartFormDataContent
            {
                { new StringContent(uploadRequest.OwnerId.ToString()), "ownerId" },
                { new StringContent(uploadRequest.CreatedBy.ToString()), "createdBy" },
                { new StringContent(uploadRequest.DocType), "docType" },
                { new StringContent(uploadRequest.Status), "status" },
                { new StringContent(uploadRequest.Sensitivity), "sensitivity" },
            };

            if (uploadRequest.DocumentTypeId is { } docTypeId)
            {
                content.Add(new StringContent(docTypeId.ToString()), "documentTypeId");
            }

            if (!string.IsNullOrWhiteSpace(uploadRequest.Title))
            {
                content.Add(new StringContent(uploadRequest.Title), "title");
            }

            if (!string.IsNullOrWhiteSpace(uploadRequest.FlowDefinition))
            {
                content.Add(new StringContent(uploadRequest.FlowDefinition), "flowDefinition");
            }

            foreach (var tagId in uploadRequest.TagIds)
            {
                if (tagId == Guid.Empty)
                {
                    continue;
                }

                content.Add(new StringContent(tagId.ToString()), "Tags");
            }

            foreach (var file in uploadRequest.Files)
            {
                var stream = File.OpenRead(file.FilePath);
                fileStreams.Add(stream);

                var fileContent = new StreamContent(stream);
                fileContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType ?? "application/octet-stream");

                var fileName = string.IsNullOrWhiteSpace(file.FileName)
                    ? Path.GetFileName(file.FilePath)
                    : file.FileName;

                content.Add(fileContent, "Files", fileName);
            }

            using var response = await _httpClient.PostAsync(GetDocumentsBatchEndpoint, content, cancellationToken);

            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError(
                    "Failed to upload document batch. Status: {StatusCode}. Body: {Body}",
                    response.StatusCode,
                    body);
                throw new HttpRequestException($"Batch upload failed with status {(int)response.StatusCode}: {body}");
            }

            var result = JsonSerializer.Deserialize<DocumentBatchResult>(body, _jsonOptions);
            return result is null ? throw new HttpRequestException("Batch upload succeeded but response body was empty.") : result;
        }
        finally
        {
            foreach (var stream in fileStreams)
            {
                await stream.DisposeAsync();
            }
        }
    }

    /// <summary>
    /// Retrieves detailed information for a specific document.
    /// </summary>
    /// <param name="documentId">Identifier of the document to retrieve.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>The document details when found; otherwise, <c>null</c>.</returns>
    public async Task<DocumentDto?> GetDocumentAsync(Guid documentId, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.GetAsync($"{GetDocumentsEndpoint}/{documentId}", cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            _logger.LogWarning("Document {DocumentId} không tồn tại.", documentId);
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<DocumentDto>(cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Requests a download URI for the specified document version.
    /// </summary>
    /// <param name="versionId">Identifier of the document version to download.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>An absolute or relative URI when available; otherwise, <c>null</c>.</returns>
    public async Task<Uri?> GetDownloadUriAsync(Guid versionId, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, $"{GetDownloadEndpoint}/{versionId}");
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

    /// <summary>
    /// Downloads the binary content for the specified document version.
    /// </summary>
    /// <param name="versionId">Identifier of the document version.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>The download payload when successful; otherwise, <c>null</c> if missing.</returns>
    public async Task<DocumentDownloadResult?> DownloadVersionAsync(Guid versionId, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.GetAsync(
            $"{GetDownloadEndpoint}/{versionId}",
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            _logger.LogWarning("Version {VersionId} không tồn tại khi tải.", versionId);
            return null;
        }

        response.EnsureSuccessStatusCode();

        await using var networkStream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var buffer = new MemoryStream();
        await networkStream.CopyToAsync(buffer, cancellationToken);
        buffer.Position = 0;

        var contentType = response.Content.Headers.ContentType?.ToString() ?? "application/octet-stream";
        var fileName = response.Content.Headers.ContentDisposition?.FileNameStar
            ?? response.Content.Headers.ContentDisposition?.FileName;
        var lastModified = response.Content.Headers.LastModified;

        return new DocumentDownloadResult(buffer, contentType, fileName, lastModified);
    }

    /// <summary>
    /// Retrieves all tags available to the current user.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>A collection of tag labels.</returns>
    public async Task<IReadOnlyCollection<TagLabelDto>> ListTagsAsync(CancellationToken cancellationToken)
    {   
        using var response = await _httpClient.GetAsync(GetTagsEndpoint, cancellationToken);
        response.EnsureSuccessStatusCode();

        var tags = await response.Content.ReadFromJsonAsync<TagLabelDto[]>(cancellationToken: cancellationToken);
        return tags ?? [];
    }

    /// <summary>
    /// Creates a new tag.
    /// </summary>
    /// <param name="request">Details describing the tag to create.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>The created tag information.</returns>
    public async Task<TagLabelDto?> CreateTagAsync(TagCreateRequest request, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.PostAsJsonAsync(GetTagsEndpoint, request, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<TagLabelDto>(cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Updates an existing tag.
    /// </summary>
    /// <param name="tagId">Identifier of the tag to update.</param>
    /// <param name="request">Updated tag values.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>The updated tag when successful; otherwise, <c>null</c> if not found or forbidden.</returns>
    public async Task<TagLabelDto?> UpdateTagAsync(Guid tagId, TagUpdateRequest request, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.PutAsJsonAsync($"{GetTagsEndpoint}/{tagId}", request, cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            _logger.LogWarning("Không tìm thấy tag {TagId} khi cập nhật.", tagId);
            return null;
        }

        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<TagLabelDto>(cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Deletes an existing tag.
    /// </summary>
    /// <param name="tagId">Identifier of the tag to delete.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns><c>true</c> when the tag was deleted; otherwise, <c>false</c> when missing.</returns>
    public async Task<bool> DeleteTagAsync(Guid tagId, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.DeleteAsync($"{GetTagsEndpoint}/{tagId}", cancellationToken);

        if (response.StatusCode is HttpStatusCode.NotFound)
        {
            _logger.LogWarning("Không tìm thấy tag {TagId} khi xoá.", tagId);
            return false;
        }

        response.EnsureSuccessStatusCode();
        return true;
    }

    /// <summary>
    /// Assigns an existing tag to a document.
    /// </summary>
    /// <param name="documentId">Identifier of the document receiving the tag.</param>
    /// <param name="tagId">Identifier of the tag to assign.</param>
    /// <param name="appliedBy">User applying the tag.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns><c>true</c> when the assignment succeeds; otherwise, <c>false</c>.</returns>
    public async Task<bool> AssignTagToDocumentAsync(
        Guid documentId,
        Guid tagId,
        Guid? appliedBy,
        CancellationToken cancellationToken)
    {
        using var response = await _httpClient.PostAsJsonAsync(
            GetDocumentTagsEndpoint(documentId),
            new AssignTagRequest(tagId, appliedBy),
            cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            return true;
        }

        _logger.LogWarning(
            "Không thể gán tag {TagId} cho document {DocumentId}. Status: {Status}",
            tagId,
            documentId,
            response.StatusCode);
        return false;
    }

    /// <summary>
    /// Removes an assigned tag from a document.
    /// </summary>
    /// <param name="documentId">Identifier of the document.</param>
    /// <param name="tagId">Identifier of the tag assignment to remove.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns><c>true</c> when the tag was removed; otherwise, <c>false</c>.</returns>
    public async Task<bool> RemoveTagFromDocumentAsync(Guid documentId, Guid tagId, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.DeleteAsync(
            $"{GetDocumentTagsEndpoint(documentId)}/{tagId}",
            cancellationToken);

        if (response.IsSuccessStatusCode)
        {
            return true;
        }

        _logger.LogWarning(
            "Không thể xoá tag {TagId} khỏi document {DocumentId}. Status: {Status}",
            tagId,
            documentId,
            response.StatusCode);
        return false;
    }

    /// <summary>
    /// Retrieves a paged list of documents using the supplied filters.
    /// </summary>
    /// <param name="query">Query filters for the search.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>The result set of documents.</returns>
    public async Task<DocumentListResult?> ListDocumentsAsync(DocumentListQuery query, CancellationToken cancellationToken)
    {
        var url = AppendQueryString(GetDocumentsEndpoint, BuildDocumentQueryParameters(query));
        using var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<DocumentListResult>(cancellationToken: cancellationToken);
    }

    /// <summary>
    /// Updates document metadata.
    /// </summary>
    /// <param name="documentId">Identifier of the document to update.</param>
    /// <param name="request">New document values.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns>The updated document list item when successful; otherwise, <c>null</c>.</returns>
    public async Task<DocumentListItem?> UpdateDocumentAsync(Guid documentId, DocumentUpdateRequest request, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.PutAsJsonAsync($"{GetDocumentsEndpoint}/{documentId}", request, cancellationToken);

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

    /// <summary>
    /// Deletes a document if the current user has permission.
    /// </summary>
    /// <param name="documentId">Identifier of the document to delete.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns><c>true</c> when the document was deleted; otherwise, <c>false</c> when missing or forbidden.</returns>
    public async Task<bool> DeleteDocumentAsync(Guid documentId, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.DeleteAsync($"{GetDocumentsEndpoint}/{documentId}", cancellationToken);

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

    /// <summary>
    /// Deletes a document by specifying one of its version identifiers.
    /// </summary>
    /// <param name="versionId">Identifier of the version belonging to the document.</param>
    /// <param name="cancellationToken">Token used to cancel the request.</param>
    /// <returns><c>true</c> when the document was deleted; otherwise, <c>false</c> when missing or forbidden.</returns>
    public async Task<bool> DeleteDocumentByVersionAsync(Guid versionId, CancellationToken cancellationToken)
    {
        using var response = await _httpClient.DeleteAsync($"{GetFileManagementEndpoint}/{versionId}", cancellationToken);

        if (response.StatusCode is HttpStatusCode.NotFound)
        {
            _logger.LogWarning("Document version {VersionId} không tồn tại khi xoá.", versionId);
            return false;
        }

        if (response.StatusCode is HttpStatusCode.Forbidden)
        {
            _logger.LogWarning("Không đủ quyền xoá tài liệu chứa version {VersionId}.", versionId);
            return false;
        }

        response.EnsureSuccessStatusCode();
        return true;
    }

    private const string GetDocumentsEndpoint = "api/documents";

    private const string GetDocumentsBatchEndpoint = "api/documents/batch";

    private const string GetDownloadEndpoint = "api/documents/files/download";

    private const string GetFileManagementEndpoint = "api/documents/files";

    private const string GetTagsEndpoint = "api/tags";

    private static string GetDocumentTagsEndpoint(Guid documentId) => $"api/documents/{documentId}/tags";

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

    private static void AddIfNotEmpty(Dictionary<string, string?> parameters, string key, string? value)
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
