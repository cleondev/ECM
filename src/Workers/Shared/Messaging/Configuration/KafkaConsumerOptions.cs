namespace Workers.Shared.Messaging;

/// <summary>
///     Configuration options for the <see cref="KafkaConsumer"/> used by the background workers.
/// </summary>
public sealed class KafkaConsumerOptions
{
    /// <summary>
    ///     The configuration section name expected in appsettings.json or environment variables.
    /// </summary>
    public const string SectionName = "Kafka";

    /// <summary>
    ///     Gets or sets the bootstrap servers (host:port pairs) that the consumer should connect to.
    /// </summary>
    public string? BootstrapServers { get; set; }

    /// <summary>
    ///     Gets or sets the consumer group id.
    /// </summary>
    public string? GroupId { get; set; }

    /// <summary>
    ///     Gets or sets a value indicating whether the consumer should commit offsets automatically.
    /// </summary>
    public bool EnableAutoCommit { get; set; } = true;

    /// <summary>
    ///     Gets or sets the client identifier reported to the Kafka broker. When omitted a value derived
    ///     from the group id and machine name is used.
    /// </summary>
    public string? ClientId { get; set; }

    /// <summary>
    ///     Gets or sets the auto offset reset behavior used when the consumer group has no committed offset.
    ///     Accepts the same values as Confluent's <c>auto.offset.reset</c> configuration key (Earliest, Latest, Error).
    /// </summary>
    public string AutoOffsetReset { get; set; } = "Earliest";

    /// <summary>
    ///     Gets or sets any additional configuration key/value pairs that should be passed to the Kafka consumer.
    ///     This allows advanced scenarios (SASL, TLS, custom timeouts, ...).
    /// </summary>
    public Dictionary<string, string> AdditionalConfig { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
