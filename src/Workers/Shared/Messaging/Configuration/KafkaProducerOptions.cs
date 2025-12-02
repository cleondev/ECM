namespace Workers.Shared.Messaging;

/// <summary>
///     Configuration options for the Kafka producer used by the background workers.
/// </summary>
public sealed class KafkaProducerOptions
{
    public const string SectionName = "Kafka";

    /// <summary>
    ///     Gets or sets the bootstrap servers used to connect to the Kafka cluster.
    /// </summary>
    public string? BootstrapServers { get; set; }

    /// <summary>
    ///     Gets or sets the client identifier reported to the Kafka broker.
    /// </summary>
    public string? ClientId { get; set; }

    /// <summary>
    ///     Gets or sets any additional configuration values passed to the producer.
    /// </summary>
    public IDictionary<string, string>? AdditionalOptions { get; set; }
}
