namespace ECM.Modules.Document.Domain.Documents;

public sealed class DocumentTitle
{
    private DocumentTitle(string value)
    {
        Value = value;
    }

    public string Value { get; }

    public static DocumentTitle Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Document title is required.", nameof(value));
        }

        return new DocumentTitle(value.Trim());
    }

    public override string ToString() => Value;
}
