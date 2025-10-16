using System.Linq;
using System.Text.Json;
using ECM.IAM.Domain.Relations.Events;
using ECM.IAM.Domain.Users.Events;
using ECM.BuildingBlocks.Domain.Events;
using Shared.Contracts.IAM;

namespace ECM.IAM.Infrastructure.Outbox;

internal static class IamOutboxMapper
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
            UserCreatedDomainEvent created => Map(created),
            UserRoleAssignedDomainEvent roleAssigned => Map(roleAssigned),
            UserRoleRemovedDomainEvent roleRemoved => Map(roleRemoved),
            AccessRelationCreatedDomainEvent relationCreated => Map(relationCreated),
            AccessRelationDeletedDomainEvent relationDeleted => Map(relationDeleted),
            _ => null
        };
    }

    private static OutboxMessage Map(UserCreatedDomainEvent domainEvent)
    {
        var contract = new UserCreatedContract(
            domainEvent.UserId,
            domainEvent.Email,
            domainEvent.DisplayName,
            domainEvent.Department,
            domainEvent.IsActive,
            domainEvent.OccurredAtUtc);

        var payload = JsonSerializer.Serialize(contract, SerializerOptions);

        return new OutboxMessage(
            aggregate: "user",
            aggregateId: domainEvent.UserId,
            type: IamEventNames.UserCreated,
            payload: payload,
            occurredAtUtc: domainEvent.OccurredAtUtc);
    }

    private static OutboxMessage Map(UserRoleAssignedDomainEvent domainEvent)
    {
        var contract = new UserRoleAssignedContract(
            domainEvent.UserId,
            domainEvent.RoleId,
            domainEvent.RoleName,
            domainEvent.OccurredAtUtc);

        var payload = JsonSerializer.Serialize(contract, SerializerOptions);

        return new OutboxMessage(
            aggregate: "user",
            aggregateId: domainEvent.UserId,
            type: IamEventNames.UserRoleAssigned,
            payload: payload,
            occurredAtUtc: domainEvent.OccurredAtUtc);
    }

    private static OutboxMessage Map(UserRoleRemovedDomainEvent domainEvent)
    {
        var contract = new UserRoleRemovedContract(
            domainEvent.UserId,
            domainEvent.RoleId,
            domainEvent.OccurredAtUtc);

        var payload = JsonSerializer.Serialize(contract, SerializerOptions);

        return new OutboxMessage(
            aggregate: "user",
            aggregateId: domainEvent.UserId,
            type: IamEventNames.UserRoleRemoved,
            payload: payload,
            occurredAtUtc: domainEvent.OccurredAtUtc);
    }

    private static OutboxMessage Map(AccessRelationCreatedDomainEvent domainEvent)
    {
        var contract = new AccessRelationCreatedContract(
            domainEvent.SubjectId,
            domainEvent.ObjectType,
            domainEvent.ObjectId,
            domainEvent.Relation,
            domainEvent.OccurredAtUtc);

        var payload = JsonSerializer.Serialize(contract, SerializerOptions);

        return new OutboxMessage(
            aggregate: "access-relation",
            aggregateId: CreateDeterministicGuid(domainEvent.SubjectId, domainEvent.ObjectType, domainEvent.ObjectId, domainEvent.Relation),
            type: IamEventNames.AccessRelationCreated,
            payload: payload,
            occurredAtUtc: domainEvent.OccurredAtUtc);
    }

    private static OutboxMessage Map(AccessRelationDeletedDomainEvent domainEvent)
    {
        var contract = new AccessRelationDeletedContract(
            domainEvent.SubjectId,
            domainEvent.ObjectType,
            domainEvent.ObjectId,
            domainEvent.Relation,
            domainEvent.OccurredAtUtc);

        var payload = JsonSerializer.Serialize(contract, SerializerOptions);

        return new OutboxMessage(
            aggregate: "access-relation",
            aggregateId: CreateDeterministicGuid(domainEvent.SubjectId, domainEvent.ObjectType, domainEvent.ObjectId, domainEvent.Relation),
            type: IamEventNames.AccessRelationDeleted,
            payload: payload,
            occurredAtUtc: domainEvent.OccurredAtUtc);
    }

    private static Guid CreateDeterministicGuid(Guid subjectId, string objectType, Guid objectId, string relation)
    {
        var composite = $"{subjectId:N}:{objectType}:{objectId:N}:{relation}".ToLowerInvariant();
        var bytes = System.Text.Encoding.UTF8.GetBytes(composite);
        var hash = System.Security.Cryptography.SHA256.HashData(bytes);
        Span<byte> guidBytes = stackalloc byte[16];
        hash.AsSpan(0, 16).CopyTo(guidBytes);
        return new Guid(guidBytes);
    }
}
