using System.Collections.Concurrent;
using ECM.Modules.Document.Domain.Documents;
using DocumentEntity = ECM.Modules.Document.Domain.Documents.Document;

namespace ECM.Modules.Document.Infrastructure.Documents;

public sealed class InMemoryDocumentRepository : IDocumentRepository
{
    private static readonly ConcurrentDictionary<Guid, DocumentEntity> Storage = new();

    public Task<DocumentEntity> AddAsync(DocumentEntity document, CancellationToken cancellationToken = default)
    {
        Storage[document.Id.Value] = document;
        return Task.FromResult(document);
    }
}
