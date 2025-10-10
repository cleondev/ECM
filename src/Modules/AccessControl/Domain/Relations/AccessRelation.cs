namespace ECM.Modules.AccessControl.Domain.Relations;

public sealed class AccessRelation
{
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

        return new AccessRelation(
            subjectId,
            objectType.Trim(),
            objectId,
            relation.Trim(),
            createdAtUtc);
    }
}
