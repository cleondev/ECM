using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace samples.EcmFileIntegrationSample;

public sealed class EcmFileClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<EcmFileClient> _logger;

    public EcmFileClient(HttpClient httpClient, ILogger<EcmFileClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<UserProfile?> GetCurrentUserProfileAsync(CancellationToken cancellationToken)
    {
        using var response = await _httpClient.GetAsync("api/iam/profile", cancellationToken);
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

        using var response = await _httpClient.PostAsync("api/ecm/documents", content, cancellationToken);

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
        using var request = new HttpRequestMessage(HttpMethod.Get, $"api/ecm/files/download/{versionId}");
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

    private static bool IsRedirect(HttpStatusCode statusCode) =>
        statusCode is HttpStatusCode.Moved or HttpStatusCode.Redirect or HttpStatusCode.RedirectKeepVerb or HttpStatusCode.RedirectMethod;
}
