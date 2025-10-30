using System;

namespace Workers.Shared.Messaging;

/// <summary>
///     Represents a Kafka message delivered to a background worker. Only the properties required for the tests
///     are modelled. The value is treated as UTF-8 encoded JSON.
/// </summary>
public sealed record KafkaMessage(string Topic, string? Key, string Value, DateTimeOffset Timestamp);
