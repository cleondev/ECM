namespace Workers.Shared.Messaging;

/// <summary>
///     Abstraction over the Kafka/Redpanda consumer used by the background workers. The interface allows the
///     application layer to remain agnostic from the actual messaging implementation while still enabling the worker
///     to subscribe to topics published by the rest of the platform.
/// </summary>
public interface IKafkaConsumer
{
    Task ConsumeAsync(string topic, Func<KafkaMessage, CancellationToken, Task> handler, CancellationToken cancellationToken = default);
}
