using System;

namespace Ocr.Messaging;

public sealed record KafkaMessage(string Topic, string? Key, string Value, DateTimeOffset Timestamp);
