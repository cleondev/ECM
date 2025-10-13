using System;

namespace SearchIndexer.Messaging;

/// <summary>
///     Represents a Kafka message delivered to the search indexer. Only the properties required for the current tests
///     are modelled. The value is treated as UTF-8 encoded JSON.
/// </summary>
public sealed record KafkaMessage(string Topic, string? Key, string Value, DateTimeOffset Timestamp);
