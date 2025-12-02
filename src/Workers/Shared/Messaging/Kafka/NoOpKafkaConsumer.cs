using Microsoft.Extensions.Logging;

namespace Workers.Shared.Messaging;

/// <summary>
///     Placeholder Kafka consumer used in the development environment where a real broker is not available. The
///     consumer simply keeps the subscription alive until cancellation is requested.
/// </summary>
public sealed class NoOpKafkaConsumer(ILogger<NoOpKafkaConsumer> logger) : IKafkaConsumer
{
    private readonly ILogger<NoOpKafkaConsumer> _logger = logger;

    public async Task ConsumeAsync(
        string topic,
        Func<KafkaMessage, CancellationToken, Task> handler,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(topic))
        {
            throw new ArgumentException("Topic must be provided.", nameof(topic));
        }

        ArgumentNullException.ThrowIfNull(handler);

        _logger.LogInformation(
            "No-op Kafka consumer subscribed to topic {Topic}. Waiting for cancellation.",
            topic);

        try
        {
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            // Cancellation requested, nothing else to do.
        }
    }
}
