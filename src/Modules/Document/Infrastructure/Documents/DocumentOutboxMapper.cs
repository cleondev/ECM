using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using ECM.BuildingBlocks.Domain.Events;
using ECM.Document.Domain.Documents.Events;
using ECM.Document.Infrastructure.Outbox;
using Shared.Contracts.Documents;

namespace ECM.Document.Infrastructure.Documents;

internal static class DocumentOutboxMapper
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public static IEnumerable<OutboxMessage> ToOutboxMessages(IEnumerable<IDomainEvent> domainEvents)
    {
        ArgumentNullException.ThrowIfNull(domainEvents);
        return domainEvents.Select(ToOutboxMessage).OfType<OutboxMessage>();
    }

    private static OutboxMessage? ToOutboxMessage(IDomainEvent domainEvent)
    {
        return domainEvent switch
        {
            DocumentCreatedDomainEvent created => Map(created),
            _ => null
        };
    }

    private static OutboxMessage Map(DocumentCreatedDomainEvent domainEvent)
    {
        var contract = new DocumentCreatedContract(
            domainEvent.DocumentId.Value,
            domainEvent.Title,
            domainEvent.OccurredAtUtc);

        var payload = JsonSerializer.Serialize(contract, SerializerOptions);

        return new OutboxMessage(
            aggregate: "document",
            aggregateId: domainEvent.DocumentId.Value,
            type: nameof(DocumentCreatedContract),
            payload: payload,
            occurredAtUtc: domainEvent.OccurredAtUtc);
    }
}
