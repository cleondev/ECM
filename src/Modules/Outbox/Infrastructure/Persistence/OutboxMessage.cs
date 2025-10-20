using System;

namespace ECM.Outbox.Infrastructure.Persistence;

public sealed class OutboxMessage
{
    private OutboxMessage()
    {
        Aggregate = string.Empty;
        Type = string.Empty;
        Payload = string.Empty;
    }

    public OutboxMessage(string aggregate, Guid aggregateId, string type, string payload, DateTimeOffset occurredAtUtc)
    {
        Aggregate = aggregate;
        AggregateId = aggregateId;
        Type = type;
        Payload = payload;
        OccurredAtUtc = occurredAtUtc;
    }

    public long Id { get; private set; }

    public string Aggregate { get; private set; }

    public Guid AggregateId { get; private set; }

    public string Type { get; private set; }

    public string Payload { get; private set; }

    public DateTimeOffset OccurredAtUtc { get; private set; }

    public DateTimeOffset? ProcessedAtUtc { get; private set; }
}
