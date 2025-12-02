using Confluent.Kafka;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Workers.Shared.Messaging;

/// <summary>
///     Kafka implementation of <see cref="IKafkaConsumer"/> backed by Confluent's .NET client.
/// </summary>
public sealed class KafkaConsumer : IKafkaConsumer
{
    private readonly KafkaConsumerOptions _options;
    private readonly ILogger<KafkaConsumer> _logger;

    public KafkaConsumer(IOptions<KafkaConsumerOptions> options, ILogger<KafkaConsumer> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);

        _options = options.Value;
        _logger = logger;

        if (string.IsNullOrWhiteSpace(_options.BootstrapServers))
        {
            throw new InvalidOperationException(
                "Kafka bootstrap servers are not configured. Set Kafka:BootstrapServers or the Kafka__BootstrapServers environment variable.");
        }
    }

    /// <inheritdoc />
    public Task ConsumeAsync(
        string topic,
        Func<KafkaMessage, CancellationToken, Task> handler,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(topic);
        ArgumentNullException.ThrowIfNull(handler);

        return Task.Run(() => ConsumeTopicLoopAsync(topic, handler, cancellationToken), CancellationToken.None);
    }

    private async Task ConsumeTopicLoopAsync(
        string topic,
        Func<KafkaMessage, CancellationToken, Task> handler,
        CancellationToken cancellationToken)
    {
        var config = BuildConsumerConfig();

        using var consumer = new ConsumerBuilder<string, string>(config)
            .SetErrorHandler((_, error) =>
            {
                if (error.IsError)
                {
                    _logger.LogError(
                        "Kafka consumer error on topic {Topic}: {Reason} (fatal: {IsFatal}).",
                        topic,
                        error.Reason,
                        error.IsFatal);
                }
            })
            .SetLogHandler((_, message) =>
            {
                if (message.Level <= SyslogLevel.Info)
                {
                    _logger.LogInformation(
                        "Kafka client log ({Facility}): {LogMessage}",
                        message.Facility,
                        message.Message);
                }
            })
            .Build();

        consumer.Subscribe(topic);

        _logger.LogInformation(
            "Kafka consumer subscribed to topic {Topic} with group {GroupId}.",
            topic,
            config.GroupId);

        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                ConsumeResult<string, string>? result;

                try
                {
                    result = consumer.Consume(cancellationToken);
                }
                catch (ConsumeException exception)
                {
                    _logger.LogError(exception, "Kafka consume exception on topic {Topic}.", topic);
                    continue;
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    break;
                }

                if (result is null)
                {
                    continue;
                }

                if (result.Message is null || result.Message.Value is null)
                {
                    _logger.LogWarning(
                        "Kafka consumer received empty message from topic {Topic} (partition {Partition}, offset {Offset}).",
                        topic,
                        result.Partition,
                        result.Offset);
                    continue;
                }

                var timestamp = DateTimeOffset.FromUnixTimeMilliseconds(result.Message.Timestamp.UnixTimestampMs);

                var kafkaMessage = new KafkaMessage(
                    result.Topic,
                    result.Message.Key,
                    result.Message.Value,
                    timestamp);

                try
                {
                    await handler(kafkaMessage, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception exception)
                {
                    _logger.LogError(
                        exception,
                        "Unhandled exception while processing Kafka message from topic {Topic}, partition {Partition}, offset {Offset}.",
                        result.Topic,
                        result.Partition,
                        result.Offset);
                }
            }
        }
        finally
        {
            consumer.Close();
            _logger.LogInformation("Kafka consumer unsubscribed from topic {Topic}.", topic);
        }
    }

    private ConsumerConfig BuildConsumerConfig()
    {
        if (string.IsNullOrWhiteSpace(_options.GroupId))
        {
            throw new InvalidOperationException(
                "Kafka consumer group id is not configured. Set Kafka:GroupId or the Kafka__GroupId environment variable.");
        }

        var groupId = _options.GroupId!;

        var clientId = !string.IsNullOrWhiteSpace(_options.ClientId)
            ? _options.ClientId!
            : $"{groupId}-{Environment.MachineName}";

        var config = _options.AdditionalConfig is { Count: > 0 }
            ? new ConsumerConfig(_options.AdditionalConfig)
            : new ConsumerConfig();

        config.BootstrapServers = _options.BootstrapServers;
        config.GroupId = groupId;
        config.EnableAutoCommit = _options.EnableAutoCommit;
        config.ClientId = clientId;

        if (Enum.TryParse<AutoOffsetReset>(_options.AutoOffsetReset, ignoreCase: true, out var offsetReset))
        {
            config.AutoOffsetReset = offsetReset;
        }
        else
        {
            config.AutoOffsetReset = AutoOffsetReset.Earliest;
        }

        return config;
    }
}
