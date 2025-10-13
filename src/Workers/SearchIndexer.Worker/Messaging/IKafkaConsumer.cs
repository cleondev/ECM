using System;
using System.Threading;
using System.Threading.Tasks;

namespace SearchIndexer.Messaging;

/// <summary>
///     Abstraction over the Kafka/Redpanda consumer used by the search indexer worker. The interface allows the
///     application layer to remain agnostic from the actual messaging implementation while still enabling the worker
///     to subscribe to topics published by the rest of the platform.
/// </summary>
public interface IKafkaConsumer
{
    Task ConsumeAsync(string topic, Func<KafkaMessage, CancellationToken, Task> handler, CancellationToken cancellationToken = default);
}
