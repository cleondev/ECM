using System;
using System.Collections.Generic;

namespace Ocr.Messaging;

public sealed class KafkaConsumerOptions
{
    public const string SectionName = "Kafka";

    public string? BootstrapServers { get; set; }

    public string? GroupId { get; set; }

    public bool EnableAutoCommit { get; set; } = true;

    public string? ClientId { get; set; }

    public string AutoOffsetReset { get; set; } = "Earliest";

    public Dictionary<string, string> AdditionalConfig { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}
