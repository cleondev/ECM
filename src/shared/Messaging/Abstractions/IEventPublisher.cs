using System.Threading;
using System.Threading.Tasks;
using Shared.Contracts.Documents;

namespace Shared.Messaging.Abstractions;

public interface IEventPublisher
{
    Task PublishDocumentCreatedAsync(DocumentCreatedContract contract, CancellationToken cancellationToken = default);
}
