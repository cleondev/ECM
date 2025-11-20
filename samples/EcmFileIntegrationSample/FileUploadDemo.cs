using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace samples.EcmFileIntegrationSample;

public sealed class FileUploadDemo(
    EcmFileClient client,
    IHostApplicationLifetime lifetime,
    IOptions<EcmIntegrationOptions> options,
    ILogger<FileUploadDemo> logger) : BackgroundService
{
    private readonly EcmFileClient _client = client;
    private readonly IHostApplicationLifetime _lifetime = lifetime;
    private readonly EcmIntegrationOptions _options = options.Value;
    private readonly ILogger<FileUploadDemo> _logger = logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            _logger.LogInformation("ECM integration sample started using base URL {BaseUrl}.", _options.BaseUrl);

            var profile = await _client.GetCurrentUserProfileAsync(stoppingToken);
            if (profile is null)
            {
                _logger.LogError("Unable to resolve current user profile. Ensure authentication is configured correctly.");
                return;
            }

            _logger.LogInformation("Authenticated as {Email} ({UserId}).", profile.Email, profile.Id);

            var ownerId = _options.OwnerId ?? profile.Id;
            var createdBy = _options.CreatedBy ?? profile.Id;

            if (!File.Exists(_options.FilePath))
            {
                _logger.LogError(
                    "File {FilePath} does not exist. Update Ecm:FilePath in configuration before rerunning.",
                    _options.FilePath);
                return;
            }

            var uploadRequest = new DocumentUploadRequest(
                ownerId,
                createdBy,
                _options.DocType,
                _options.Status,
                _options.Sensitivity,
                _options.FilePath)
            {
                DocumentTypeId = _options.DocumentTypeId,
                Title = string.IsNullOrWhiteSpace(_options.Title) ? Path.GetFileName(_options.FilePath) : _options.Title,
            };

            _logger.LogInformation("Uploading {File} as owner {OwnerId}…", uploadRequest.FilePath, uploadRequest.OwnerId);
            var document = await _client.UploadDocumentAsync(uploadRequest, stoppingToken);

            if (document is null)
            {
                _logger.LogError("ECM did not return a document payload after upload.");
                return;
            }

            _logger.LogInformation(
                "Upload succeeded. Document {DocumentId} now has {ByteCount} bytes in version {VersionId}.",
                document.Id,
                document.LatestVersion?.Bytes,
                document.LatestVersion?.Id);

            if (document.LatestVersion is { } version)
            {
                var downloadUri = await _client.GetDownloadUriAsync(version.Id, stoppingToken);
                if (downloadUri is not null)
                {
                    _logger.LogInformation("Download URL: {Uri}", downloadUri);
                }
            }
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "ECM integration sample failed.");
        }
        finally
        {
            _lifetime.StopApplication();
        }
    }
}
