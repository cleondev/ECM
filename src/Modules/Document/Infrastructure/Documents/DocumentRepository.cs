using ECM.Document.Application.Documents.Repositories;
using ECM.Document.Domain.Documents;
using ECM.Document.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ECM.Document.Infrastructure.Documents;

public sealed class DocumentRepository(DocumentDbContext context) : IDocumentRepository
{
    private readonly DocumentDbContext _context = context;

    public async Task<Document> AddAsync(Document document, CancellationToken cancellationToken = default)
    {
        await _context.Documents.AddAsync(document, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return document;
    }

    public Task<Document?> GetAsync(DocumentId documentId, CancellationToken cancellationToken = default)
    {
        return _context.Documents
            .Include(document => document.Tags)
            .FirstOrDefaultAsync(document => document.Id == documentId, cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        => _context.SaveChangesAsync(cancellationToken);
}
