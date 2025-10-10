using DocumentAggregate = ECM.Document.Domain.Documents.Document;

namespace ECM.Document.Domain.Documents;

public interface IDocumentRepository
{
    Task<DocumentAggregate> AddAsync(DocumentAggregate document, CancellationToken cancellationToken = default);
}
