using ECM.Document.Domain.Documents;
using DomainDocument = ECM.Document.Domain.Documents.Document;

namespace ECM.Document.Application.Documents.Repositories;

public interface IDocumentRepository
{
    Task<DomainDocument> AddAsync(DomainDocument document, CancellationToken cancellationToken = default);

    Task<DomainDocument?> GetAsync(DocumentId documentId, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
