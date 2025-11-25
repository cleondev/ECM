namespace Ecm.FileIntegration;

public sealed record TagLabelDto(
    Guid Id,
    Guid NamespaceId,
    string? NamespaceScope,
    string? NamespaceDisplayName,
    Guid? ParentId,
    string Name,
    IReadOnlyCollection<Guid> PathIds,
    int SortOrder,
    string? Color,
    string? IconKey,
    bool IsActive,
    bool IsSystem,
    Guid? CreatedBy,
    DateTimeOffset CreatedAtUtc);

public sealed record TagCreateRequest(
    Guid? NamespaceId,
    Guid? ParentId,
    string Name,
    int? SortOrder,
    string? Color,
    string? IconKey,
    Guid? CreatedBy,
    bool IsSystem = false);

public sealed record TagUpdateRequest(
    Guid NamespaceId,
    Guid? ParentId,
    string Name,
    int? SortOrder,
    string? Color,
    string? IconKey,
    bool IsActive,
    Guid? UpdatedBy);
