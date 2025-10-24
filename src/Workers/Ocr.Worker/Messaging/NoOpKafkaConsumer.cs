using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Ocr.Messaging;

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
