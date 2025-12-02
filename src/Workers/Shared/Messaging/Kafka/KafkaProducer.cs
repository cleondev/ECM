using Confluent.Kafka;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Workers.Shared.Messaging;

/// <summary>
///     Kafka implementation of <see cref="IKafkaProducer"/> backed by Confluent's .NET client.
/// </summary>
public sealed class KafkaProducer : IKafkaProducer, IDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly ILogger<KafkaProducer> _logger;

    public KafkaProducer(IOptions<KafkaProducerOptions> options, ILogger<KafkaProducer> logger)
    {
        ArgumentNullException.ThrowIfNull(options);

        var value = options.Value;
        var bootstrapServers = value.BootstrapServers;

        if (string.IsNullOrWhiteSpace(bootstrapServers))
        {
            throw new InvalidOperationException(
                "Kafka bootstrap servers are not configured. Set Kafka:BootstrapServers or the Kafka__BootstrapServers environment variable.");
        }

        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        var config = BuildProducerConfig(bootstrapServers, value.ClientId, value.AdditionalOptions);
        _producer = new ProducerBuilder<string, string>(config)
            .SetErrorHandler((_, error) =>
            {
                if (!error.IsError)
                {
                    return;
                }

                _logger.LogError("Kafka producer error: {Reason} (fatal: {IsFatal}).", error.Reason, error.IsFatal);
            })
            .SetLogHandler((_, logMessage) =>
            {
                _logger.LogDebug("Kafka producer log ({Facility}): {Message}", logMessage.Facility, logMessage.Message);
            })
            .Build();
    }

    /// <inheritdoc />
    public async Task ProduceAsync(string topic, string key, string value, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(topic);
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(value);

        cancellationToken.ThrowIfCancellationRequested();

        try
        {
            var message = new Message<string, string>
            {
                Key = key,
                Value = value,
                Timestamp = Timestamp.Default
            };

            var delivery = await _producer
                .ProduceAsync(topic, message, cancellationToken)
                .ConfigureAwait(false);

            _logger.LogDebug(
                "Kafka producer delivered message to {TopicPartitionOffset}.",
                delivery.TopicPartitionOffset);
        }
        catch (ProduceException<string, string> exception)
        {
            _logger.LogError(
                exception,
                "Kafka producer encountered an error while publishing to topic {Topic}.",
                topic);
            throw;
        }
    }

    public void Dispose()
    {
        _producer.Dispose();
    }

    private static ProducerConfig BuildProducerConfig(
        string bootstrapServers,
        string? clientId,
        IDictionary<string, string>? additionalOptions)
    {
        var config = new ProducerConfig
        {
            BootstrapServers = bootstrapServers,
            ClientId = clientId,
            EnableIdempotence = true,
            Acks = Acks.All,
            MessageSendMaxRetries = 5
        };

        if (additionalOptions is null)
        {
            return config;
        }

        foreach (var (key, value) in additionalOptions)
        {
            if (string.IsNullOrWhiteSpace(key) || value is null)
            {
                continue;
            }

            config.Set(key, value);
        }

        return config;
    }
}
