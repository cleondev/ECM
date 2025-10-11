using ECM.Document.Domain.Documents;

namespace ECM.Document.Application.Documents.Repositories;

using DocumentAggregate = ECM.Document.Domain.Documents.Document;

public interface IDocumentRepository
{
    Task<DocumentAggregate> AddAsync(DocumentAggregate document, CancellationToken cancellationToken = default);

    Task<DocumentAggregate?> GetAsync(DocumentId documentId, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
