namespace ECM.Document.Domain.Documents;

public readonly record struct DocumentId(Guid Value)
{
    public static DocumentId New() => new(Guid.NewGuid());

    public static DocumentId FromGuid(Guid value) => new(value);

    public override string ToString() => Value.ToString();
}
