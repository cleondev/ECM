using DocumentAggregate = ECM.Modules.Document.Domain.Documents.Document;

namespace ECM.Modules.Document.Domain.Documents;

public interface IDocumentRepository
{
    Task<DocumentAggregate> AddAsync(DocumentAggregate document, CancellationToken cancellationToken = default);
}
