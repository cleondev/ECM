using System;
using System.Text.Json;
using ECM.Operations.Infrastructure.Persistence;

namespace ECM.Operations.Infrastructure.Outbox;

public static class OutboxMessageFactory
{
    public static JsonSerializerOptions SerializerOptions { get; } = new(JsonSerializerDefaults.Web);

    public static OutboxMessage Create<TPayload>(
        string aggregate,
        Guid aggregateId,
        string type,
        TPayload payload,
        DateTimeOffset occurredAtUtc)
    {
        var serializedPayload = JsonSerializer.Serialize(payload, SerializerOptions);

        return new OutboxMessage(
            aggregate: aggregate,
            aggregateId: aggregateId,
            type: type,
            payload: serializedPayload,
            occurredAtUtc: occurredAtUtc);
    }
}
