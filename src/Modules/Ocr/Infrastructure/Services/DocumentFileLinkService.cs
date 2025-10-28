using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ECM.Abstractions.Files;
using ECM.BuildingBlocks.Application;
using ECM.Ocr.Application.Abstractions;
using ECM.Document.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ECM.Ocr.Infrastructure.Services;

internal sealed class DocumentFileLinkService(
    DocumentDbContext documentDbContext,
    IFileAccessGateway fileAccessGateway,
    ILogger<DocumentFileLinkService> logger) : IDocumentFileLinkService
{
    private static readonly TimeSpan LinkLifetime = TimeSpan.FromMinutes(15);

    private readonly DocumentDbContext _documentDbContext = documentDbContext
        ?? throw new ArgumentNullException(nameof(documentDbContext));
    private readonly IFileAccessGateway _fileAccessGateway = fileAccessGateway
        ?? throw new ArgumentNullException(nameof(fileAccessGateway));
    private readonly ILogger<DocumentFileLinkService> _logger = logger
        ?? throw new ArgumentNullException(nameof(logger));

    public async Task<Uri?> GetDownloadLinkAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var storageKey = await _documentDbContext.DocumentVersions
            .AsNoTracking()
            .Where(version => version.DocumentId.Value == documentId)
            .OrderByDescending(version => version.VersionNo)
            .Select(version => version.StorageKey)
            .FirstOrDefaultAsync(cancellationToken)
            .ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(storageKey))
        {
            _logger.LogWarning(
                "No document version found while generating download link for document {DocumentId}.",
                documentId);
            return null;
        }

        var downloadLink = await _fileAccessGateway
            .GetDownloadLinkAsync(storageKey, LinkLifetime, cancellationToken)
            .ConfigureAwait(false);

        if (downloadLink.IsFailure || downloadLink.Value is null)
        {
            _logger.LogError(
                "Unable to generate download link for document {DocumentId}. Errors: {Errors}",
                documentId,
                string.Join(", ", downloadLink.Errors));
            return null;
        }

        return downloadLink.Value.Uri;
    }
}
