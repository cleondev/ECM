using DocumentAggregate = ECM.Document.Domain.Documents.Document;

namespace ECM.Document.Application.Documents;

public interface IDocumentRepository
{
    Task<DocumentAggregate> AddAsync(DocumentAggregate document, CancellationToken cancellationToken = default);

    Task<DocumentAggregate?> GetAsync(DocumentId documentId, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
