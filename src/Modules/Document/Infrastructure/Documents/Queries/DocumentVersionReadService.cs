using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ECM.Document.Application.Documents.Queries;
using ECM.Document.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ECM.Document.Infrastructure.Documents.Queries;

public sealed class DocumentVersionReadService(DocumentDbContext context) : IDocumentVersionReadService
{
    private readonly DocumentDbContext _context = context;

    public async Task<DocumentVersionResult?> GetByIdAsync(Guid versionId, CancellationToken cancellationToken = default)
    {
        return await _context.DocumentVersions
            .AsNoTracking()
            .Where(version => version.Id == versionId)
            .Select(version => new DocumentVersionResult(
                version.Id,
                version.DocumentId.Value,
                version.StorageKey,
                version.Bytes,
                version.MimeType,
                version.CreatedBy,
                version.CreatedAtUtc))
            .FirstOrDefaultAsync(cancellationToken);
    }
}
