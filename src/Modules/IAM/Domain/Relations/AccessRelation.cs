namespace ECM.IAM.Domain.Relations;

using ECM.IAM.Domain.Relations.Events;
using ECM.BuildingBlocks.Domain.Events;

public sealed class AccessRelation : IHasDomainEvents
{
    private readonly List<IDomainEvent> _domainEvents = [];

    private AccessRelation()
    {
        ObjectType = null!;
        Relation = null!;
    }

    private AccessRelation(
        Guid subjectId,
        string objectType,
        Guid objectId,
        string relation,
        DateTimeOffset createdAtUtc)
        : this()
    {
        SubjectId = subjectId;
        ObjectType = objectType;
        ObjectId = objectId;
        Relation = relation;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid SubjectId { get; private set; }

    public string ObjectType { get; private set; }

    public Guid ObjectId { get; private set; }

    public string Relation { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public static AccessRelation Create(
        Guid subjectId,
        string objectType,
        Guid objectId,
        string relation,
        DateTimeOffset createdAtUtc)
    {
        if (string.IsNullOrWhiteSpace(objectType))
        {
            throw new ArgumentException("Object type is required.", nameof(objectType));
        }

        if (string.IsNullOrWhiteSpace(relation))
        {
            throw new ArgumentException("Relation is required.", nameof(relation));
        }

        var accessRelation = new AccessRelation(
            subjectId,
            objectType.Trim(),
            objectId,
            relation.Trim(),
            createdAtUtc);

        accessRelation.Raise(new AccessRelationCreatedDomainEvent(
            accessRelation.SubjectId,
            accessRelation.ObjectType,
            accessRelation.ObjectId,
            accessRelation.Relation,
            createdAtUtc));

        return accessRelation;
    }

    public void MarkDeleted(DateTimeOffset deletedAtUtc)
    {
        Raise(new AccessRelationDeletedDomainEvent(SubjectId, ObjectType, ObjectId, Relation, deletedAtUtc));
    }

    public void ClearDomainEvents() => _domainEvents.Clear();

    private void Raise(IDomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        _domainEvents.Add(domainEvent);
    }
}
