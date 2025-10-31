using System.Linq;
using System.Text.Json;
using ECM.BuildingBlocks.Domain.Events;
using ECM.Document.Domain.Documents.Events;
using ECM.Document.Domain.Tags.Events;
using ECM.Operations.Infrastructure.Persistence;
using Shared.Contracts.Messaging;

namespace ECM.Document.Infrastructure.Outbox;

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
            DocumentUpdatedDomainEvent updated => Map(updated),
            DocumentDeletedDomainEvent deleted => Map(deleted),
            DocumentTagAssignedDomainEvent tagAssigned => Map(tagAssigned),
            DocumentTagRemovedDomainEvent tagRemoved => Map(tagRemoved),
            TagLabelCreatedDomainEvent tagCreated => Map(tagCreated),
            TagLabelUpdatedDomainEvent tagUpdated => Map(tagUpdated),
            TagLabelDeletedDomainEvent tagDeleted => Map(tagDeleted),
            _ => null
        };
    }

    private static OutboxMessage Map(DocumentCreatedDomainEvent domainEvent)
    {
        var contract = new DocumentCreatedContract(
            domainEvent.DocumentId.Value,
            domainEvent.Title,
            domainEvent.OwnerId,
            domainEvent.CreatedBy,
            domainEvent.OccurredAtUtc);

        var payload = JsonSerializer.Serialize(contract, SerializerOptions);

        return new OutboxMessage(
            aggregate: "document",
            aggregateId: domainEvent.DocumentId.Value,
            type: EventNames.Document.Created,
            payload: payload,
            occurredAtUtc: domainEvent.OccurredAtUtc);
    }

    private static OutboxMessage Map(DocumentUpdatedDomainEvent domainEvent)
    {
        var contract = new DocumentUpdatedContract(
            domainEvent.DocumentId.Value,
            domainEvent.Title,
            domainEvent.Status,
            domainEvent.Sensitivity,
            domainEvent.GroupId,
            domainEvent.UpdatedBy,
            domainEvent.OccurredAtUtc);

        var payload = JsonSerializer.Serialize(contract, SerializerOptions);

        return new OutboxMessage(
            aggregate: "document",
            aggregateId: domainEvent.DocumentId.Value,
            type: EventNames.Document.Updated,
            payload: payload,
            occurredAtUtc: domainEvent.OccurredAtUtc);
    }

    private static OutboxMessage Map(DocumentDeletedDomainEvent domainEvent)
    {
        var contract = new DocumentDeletedContract(
            domainEvent.DocumentId.Value,
            domainEvent.DeletedBy,
            domainEvent.OccurredAtUtc);

        var payload = JsonSerializer.Serialize(contract, SerializerOptions);

        return new OutboxMessage(
            aggregate: "document",
            aggregateId: domainEvent.DocumentId.Value,
            type: EventNames.Document.Deleted,
            payload: payload,
            occurredAtUtc: domainEvent.OccurredAtUtc);
    }

    private static OutboxMessage Map(DocumentTagAssignedDomainEvent domainEvent)
    {
        var contract = new DocumentTagAssignedContract(
            domainEvent.DocumentId.Value,
            domainEvent.TagId,
            domainEvent.AppliedBy,
            domainEvent.OccurredAtUtc);

        var payload = JsonSerializer.Serialize(contract, SerializerOptions);

        return new OutboxMessage(
            aggregate: "document",
            aggregateId: domainEvent.DocumentId.Value,
            type: EventNames.Document.TagAssigned,
            payload: payload,
            occurredAtUtc: domainEvent.OccurredAtUtc);
    }

    private static OutboxMessage Map(DocumentTagRemovedDomainEvent domainEvent)
    {
        var contract = new DocumentTagRemovedContract(
            domainEvent.DocumentId.Value,
            domainEvent.TagId,
            domainEvent.OccurredAtUtc);

        var payload = JsonSerializer.Serialize(contract, SerializerOptions);

        return new OutboxMessage(
            aggregate: "document",
            aggregateId: domainEvent.DocumentId.Value,
            type: EventNames.Document.TagRemoved,
            payload: payload,
            occurredAtUtc: domainEvent.OccurredAtUtc);
    }

    private static OutboxMessage Map(TagLabelCreatedDomainEvent domainEvent)
    {
        var contract = new TagLabelCreatedContract(
            domainEvent.TagId,
            domainEvent.NamespaceId,
            domainEvent.ParentId,
            domainEvent.Name,
            [.. domainEvent.PathIds],
            domainEvent.SortOrder,
            domainEvent.Color,
            domainEvent.IconKey,
            domainEvent.IsSystem,
            domainEvent.CreatedBy,
            domainEvent.OccurredAtUtc);

        var payload = JsonSerializer.Serialize(contract, SerializerOptions);

        return new OutboxMessage(
            aggregate: "tag-label",
            aggregateId: domainEvent.TagId,
            type: EventNames.TagLabel.Created,
            payload: payload,
            occurredAtUtc: domainEvent.OccurredAtUtc);
    }

    private static OutboxMessage Map(TagLabelDeletedDomainEvent domainEvent)
    {
        var contract = new TagLabelDeletedContract(
            domainEvent.TagId,
            domainEvent.NamespaceId,
            domainEvent.OccurredAtUtc);

        var payload = JsonSerializer.Serialize(contract, SerializerOptions);

        return new OutboxMessage(
            aggregate: "tag-label",
            aggregateId: domainEvent.TagId,
            type: EventNames.TagLabel.Deleted,
            payload: payload,
            occurredAtUtc: domainEvent.OccurredAtUtc);
    }

    private static OutboxMessage Map(TagLabelUpdatedDomainEvent domainEvent)
    {
        var contract = new TagLabelUpdatedContract(
            domainEvent.TagId,
            domainEvent.NamespaceId,
            domainEvent.ParentId,
            domainEvent.Name,
            [.. domainEvent.PathIds],
            domainEvent.SortOrder,
            domainEvent.Color,
            domainEvent.IconKey,
            domainEvent.IsActive,
            domainEvent.UpdatedBy,
            domainEvent.OccurredAtUtc);

        var payload = JsonSerializer.Serialize(contract, SerializerOptions);

        return new OutboxMessage(
            aggregate: "tag-label",
            aggregateId: domainEvent.TagId,
            type: EventNames.TagLabel.Updated,
            payload: payload,
            occurredAtUtc: domainEvent.OccurredAtUtc);
    }
}
