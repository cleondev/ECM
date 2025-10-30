using System;

namespace ECM.Operations.Infrastructure.Persistence;

public sealed class AuditEvent
{
    private AuditEvent()
    {
        Action = string.Empty;
        ObjectType = string.Empty;
        Details = string.Empty;
    }

    public AuditEvent(Guid? actorId, string action, string objectType, Guid objectId, string details, DateTimeOffset occurredAtUtc)
    {
        ActorId = actorId;
        Action = action;
        ObjectType = objectType;
        ObjectId = objectId;
        Details = details;
        OccurredAtUtc = occurredAtUtc;
    }

    public long Id { get; private set; }

    public DateTimeOffset OccurredAtUtc { get; private set; }

    public Guid? ActorId { get; private set; }

    public string Action { get; private set; }

    public string ObjectType { get; private set; }

    public Guid ObjectId { get; private set; }

    public string Details { get; private set; }
}
