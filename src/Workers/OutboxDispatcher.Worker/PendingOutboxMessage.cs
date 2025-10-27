using System;

namespace OutboxDispatcher;

/// <summary>
///     Lightweight representation of a pending outbox message fetched from the database.
/// </summary>
internal sealed record PendingOutboxMessage(
    long Id,
    string Aggregate,
    Guid AggregateId,
    string Type,
    string Payload,
    DateTimeOffset OccurredAtUtc)
{
    public override string ToString()
        => $"#{Id} {Aggregate}/{AggregateId:D} ({Type})";
}
