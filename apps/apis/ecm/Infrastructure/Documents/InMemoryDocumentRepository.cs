using System.Collections.Concurrent;
using Ecm.Domain.Documents;

namespace Ecm.Infrastructure.Documents;

public sealed class InMemoryDocumentRepository : IDocumentRepository
{
    private static readonly ConcurrentDictionary<Guid, Document> Storage = new();

    public Task<Document> AddAsync(Document document, CancellationToken cancellationToken = default)
    {
        Storage[document.Id.Value] = document;
        return Task.FromResult(document);
    }
}
