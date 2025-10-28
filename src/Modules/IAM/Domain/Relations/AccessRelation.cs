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
        SubjectType = null!;
    }

    private AccessRelation(
        string subjectType,
        Guid subjectId,
        string objectType,
        Guid objectId,
        string relation,
        DateTimeOffset createdAtUtc,
        DateTimeOffset validFromUtc,
        DateTimeOffset? validToUtc)
        : this()
    {
        SubjectType = subjectType;
        SubjectId = subjectId;
        ObjectType = objectType;
        ObjectId = objectId;
        Relation = relation;
        CreatedAtUtc = createdAtUtc;
        ValidFromUtc = validFromUtc;
        ValidToUtc = validToUtc;
    }

    public string SubjectType { get; private set; }

    public Guid SubjectId { get; private set; }

    public string ObjectType { get; private set; }

    public Guid ObjectId { get; private set; }

    public string Relation { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset ValidFromUtc { get; private set; }

    public DateTimeOffset? ValidToUtc { get; private set; }

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public static AccessRelation Create(
        string subjectType,
        Guid subjectId,
        string objectType,
        Guid objectId,
        string relation,
        DateTimeOffset createdAtUtc,
        DateTimeOffset? validFromUtc = null,
        DateTimeOffset? validToUtc = null)
    {
        if (string.IsNullOrWhiteSpace(subjectType))
        {
            throw new ArgumentException("Subject type is required.", nameof(subjectType));
        }

        var normalizedSubjectType = subjectType.Trim().ToLowerInvariant();
        if (normalizedSubjectType is not ("user" or "group"))
        {
            throw new ArgumentException("Subject type must be either 'user' or 'group'.", nameof(subjectType));
        }

        if (string.IsNullOrWhiteSpace(objectType))
        {
            throw new ArgumentException("Object type is required.", nameof(objectType));
        }

        if (string.IsNullOrWhiteSpace(relation))
        {
            throw new ArgumentException("Relation is required.", nameof(relation));
        }

        var effectiveValidFrom = validFromUtc ?? createdAtUtc;

        if (validToUtc is not null && validToUtc < effectiveValidFrom)
        {
            throw new ArgumentException("The expiration time must be greater than or equal to the start time.", nameof(validToUtc));
        }

        var accessRelation = new AccessRelation(
            normalizedSubjectType,
            subjectId,
            objectType.Trim(),
            objectId,
            relation.Trim(),
            createdAtUtc,
            effectiveValidFrom,
            validToUtc);

        accessRelation.Raise(new AccessRelationCreatedDomainEvent(
            accessRelation.SubjectType,
            accessRelation.SubjectId,
            accessRelation.ObjectType,
            accessRelation.ObjectId,
            accessRelation.Relation,
            accessRelation.ValidFromUtc,
            accessRelation.ValidToUtc,
            createdAtUtc));

        return accessRelation;
    }

    public void MarkDeleted(DateTimeOffset deletedAtUtc)
    {
        ValidToUtc = deletedAtUtc < ValidFromUtc ? ValidFromUtc : deletedAtUtc;

        Raise(new AccessRelationDeletedDomainEvent(
            SubjectType,
            SubjectId,
            ObjectType,
            ObjectId,
            Relation,
            ValidToUtc,
            deletedAtUtc));
    }

    public void ClearDomainEvents() => _domainEvents.Clear();

    private void Raise(IDomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        _domainEvents.Add(domainEvent);
    }
}
