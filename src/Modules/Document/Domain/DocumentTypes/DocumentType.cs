using System;
using System.Text.Json;
using ECM.Document.Domain.Documents;
using DomainDocument = ECM.Document.Domain.Documents.Document;

namespace ECM.Document.Domain.DocumentTypes;

public sealed class DocumentType
{
    private readonly List<DomainDocument> _documents = [];

    private DocumentType()
    {
        TypeKey = null!;
        TypeName = null!;
        Config = null!;
    }

    public DocumentType(
        Guid id,
        string typeKey,
        string typeName,
        bool isActive,
        DateTimeOffset createdAtUtc,
        string? description = null,
        JsonDocument? config = null)
        : this()
    {
        if (string.IsNullOrWhiteSpace(typeKey))
        {
            throw new ArgumentException("Type key is required.", nameof(typeKey));
        }

        if (string.IsNullOrWhiteSpace(typeName))
        {
            throw new ArgumentException("Type name is required.", nameof(typeName));
        }

        Id = id == Guid.Empty ? Guid.NewGuid() : id;
        TypeKey = typeKey.Trim();
        TypeName = typeName.Trim();
        Description = NormalizeDescription(description);
        Config = NormalizeConfig(config);
        IsActive = isActive;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; private set; }

    public string TypeKey { get; private set; }

    public string TypeName { get; private set; }

    public string? Description { get; private set; }

    public JsonDocument Config { get; private set; }

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public IReadOnlyCollection<DomainDocument> Documents => _documents.AsReadOnly();

    public void Update(string typeKey, string typeName, string? description, bool isActive, JsonDocument? config = null)
    {
        if (string.IsNullOrWhiteSpace(typeKey))
        {
            throw new ArgumentException("Type key is required.", nameof(typeKey));
        }

        if (string.IsNullOrWhiteSpace(typeName))
        {
            throw new ArgumentException("Type name is required.", nameof(typeName));
        }

        TypeKey = typeKey.Trim();
        TypeName = typeName.Trim();
        Description = NormalizeDescription(description);
        Config = config ?? Config;
        IsActive = isActive;
    }

    private static string? NormalizeDescription(string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return null;
        }

        return description.Trim();
    }

    private static JsonDocument NormalizeConfig(JsonDocument? config)
    {
        if (config is not null)
        {
            return config;
        }

        return JsonDocument.Parse("{}");
    }
}
