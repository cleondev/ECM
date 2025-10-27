using System.Threading;
using System.Threading.Tasks;

namespace Workers.Shared.Messaging;

/// <summary>
///     Abstraction over the Kafka/Redpanda producer used by the background workers.
///     The interface allows the worker code to remain testable while hiding the
///     underlying Confluent client implementation details.
/// </summary>
public interface IKafkaProducer
{
    /// <summary>
    ///     Sends a message to the specified Kafka topic using the provided key and value.
    /// </summary>
    /// <param name="topic">The destination topic.</param>
    /// <param name="key">The message key used for partitioning and ordering.</param>
    /// <param name="value">The JSON payload to send.</param>
    /// <param name="cancellationToken">Token used to observe cancellation.</param>
    Task ProduceAsync(string topic, string key, string value, CancellationToken cancellationToken = default);
}
