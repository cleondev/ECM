using System.Collections.Concurrent;
using ECM.Modules.Document.Domain.Documents;

namespace ECM.Modules.Document.Infrastructure.Documents;

public sealed class InMemoryDocumentRepository : IDocumentRepository
{
    private static readonly ConcurrentDictionary<Guid, Document> Storage = new();

    public Task<Document> AddAsync(Document document, CancellationToken cancellationToken = default)
    {
        Storage[document.Id.Value] = document;
        return Task.FromResult(document);
    }
}
