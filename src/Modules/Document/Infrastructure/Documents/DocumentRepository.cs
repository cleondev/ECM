using ECM.Modules.Document.Domain.Documents;
using ECM.Modules.Document.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace ECM.Modules.Document.Infrastructure.Documents;

public sealed class DocumentRepository : IDocumentRepository
{
    private readonly DocumentDbContext _context;

    public DocumentRepository(DocumentDbContext context)
    {
        _context = context;
    }

    public async Task<Document> AddAsync(Document document, CancellationToken cancellationToken = default)
    {
        await _context.Documents.AddAsync(document, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
        return document;
    }
}
