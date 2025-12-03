using System.Linq;
using ECM.IAM.Domain.Relations.Events;
using ECM.IAM.Domain.Users.Events;
using ECM.BuildingBlocks.Domain.Events;
using ECM.Operations.Infrastructure.Outbox;
using ECM.Operations.Infrastructure.Persistence;
using Shared.Contracts.IAM;

namespace ECM.IAM.Infrastructure.Outbox;

internal static class IamOutboxMapper
{
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
            domainEvent.IsActive,
            domainEvent.OccurredAtUtc);

        return OutboxMessageFactory.Create(
            aggregate: "user",
            aggregateId: domainEvent.UserId,
            type: IamEventNames.UserCreated,
            payload: contract,
            occurredAtUtc: domainEvent.OccurredAtUtc);
    }

    private static OutboxMessage Map(UserRoleAssignedDomainEvent domainEvent)
    {
        var contract = new UserRoleAssignedContract(
            domainEvent.UserId,
            domainEvent.RoleId,
            domainEvent.RoleName,
            domainEvent.OccurredAtUtc);

        return OutboxMessageFactory.Create(
            aggregate: "user",
            aggregateId: domainEvent.UserId,
            type: IamEventNames.UserRoleAssigned,
            payload: contract,
            occurredAtUtc: domainEvent.OccurredAtUtc);
    }

    private static OutboxMessage Map(UserRoleRemovedDomainEvent domainEvent)
    {
        var contract = new UserRoleRemovedContract(
            domainEvent.UserId,
            domainEvent.RoleId,
            domainEvent.OccurredAtUtc);

        return OutboxMessageFactory.Create(
            aggregate: "user",
            aggregateId: domainEvent.UserId,
            type: IamEventNames.UserRoleRemoved,
            payload: contract,
            occurredAtUtc: domainEvent.OccurredAtUtc);
    }

    private static OutboxMessage Map(AccessRelationCreatedDomainEvent domainEvent)
    {
        var contract = new AccessRelationCreatedContract(
            domainEvent.SubjectType,
            domainEvent.SubjectId,
            domainEvent.ObjectType,
            domainEvent.ObjectId,
            domainEvent.Relation,
            domainEvent.ValidFromUtc,
            domainEvent.ValidToUtc,
            domainEvent.OccurredAtUtc);

        return OutboxMessageFactory.Create(
            aggregate: "access-relation",
            aggregateId: CreateDeterministicGuid(domainEvent.SubjectType, domainEvent.SubjectId, domainEvent.ObjectType, domainEvent.ObjectId, domainEvent.Relation),
            type: IamEventNames.AccessRelationCreated,
            payload: contract,
            occurredAtUtc: domainEvent.OccurredAtUtc);
    }

    private static OutboxMessage Map(AccessRelationDeletedDomainEvent domainEvent)
    {
        var contract = new AccessRelationDeletedContract(
            domainEvent.SubjectType,
            domainEvent.SubjectId,
            domainEvent.ObjectType,
            domainEvent.ObjectId,
            domainEvent.Relation,
            domainEvent.ValidToUtc,
            domainEvent.OccurredAtUtc);

        return OutboxMessageFactory.Create(
            aggregate: "access-relation",
            aggregateId: CreateDeterministicGuid(domainEvent.SubjectType, domainEvent.SubjectId, domainEvent.ObjectType, domainEvent.ObjectId, domainEvent.Relation),
            type: IamEventNames.AccessRelationDeleted,
            payload: contract,
            occurredAtUtc: domainEvent.OccurredAtUtc);
    }

    private static Guid CreateDeterministicGuid(string subjectType, Guid subjectId, string objectType, Guid objectId, string relation)
    {
        var composite = $"{subjectType}:{subjectId:N}:{objectType}:{objectId:N}:{relation}".ToLowerInvariant();
        var bytes = System.Text.Encoding.UTF8.GetBytes(composite);
        var hash = System.Security.Cryptography.SHA256.HashData(bytes);
        Span<byte> guidBytes = stackalloc byte[16];
        hash.AsSpan(0, 16).CopyTo(guidBytes);
        return new Guid(guidBytes);
    }
}
