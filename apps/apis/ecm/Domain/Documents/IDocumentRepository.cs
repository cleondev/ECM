namespace Ecm.Domain.Documents;

public interface IDocumentRepository
{
    Task<Document> AddAsync(Document document, CancellationToken cancellationToken = default);
}
