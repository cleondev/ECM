using ECM.Document.Domain.Documents;
using ECM.Document.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using DocumentAggregate = ECM.Document.Domain.Documents.Document;

namespace ECM.Document.Infrastructure.Documents;

public sealed class DocumentRepository(DocumentDbContext context) : IDocumentRepository
{
    private readonly DocumentDbContext _context = context;

    public async Task<DocumentAggregate> AddAsync(DocumentAggregate document, CancellationToken cancellationToken = default)
    {
        await _context.Documents.AddAsync(document, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return document;
    }
}
