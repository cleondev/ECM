using ECM.Document.Domain.Documents;

namespace ECM.Document.Application.Documents.Repositories;

public interface IDocumentRepository
{
    Task<Document> AddAsync(Document document, CancellationToken cancellationToken = default);

    Task<Document?> GetAsync(DocumentId documentId, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
