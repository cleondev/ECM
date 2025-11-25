namespace samples.EcmFileIntegrationSample;

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

public sealed record DocumentListQuery(
    string? Query,
    string? DocType,
    string? Status,
    string? Sensitivity,
    Guid? OwnerId,
    Guid? GroupId,
    int Page,
    int PageSize);

public sealed record DocumentListResult(
    int Page,
    int PageSize,
    long TotalItems,
    int TotalPages,
    IReadOnlyCollection<DocumentListItem> Items);

public sealed record DocumentListItem(
    Guid Id,
    string Title,
    string DocType,
    string Status,
    string Sensitivity,
    Guid OwnerId,
    Guid CreatedBy,
    Guid? GroupId,
    IReadOnlyCollection<Guid> GroupIds,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    Guid? DocumentTypeId,
    DocumentVersionDto? LatestVersion,
    IReadOnlyCollection<DocumentTagDetailDto> Tags,
    string? CreatedAtFormatted,
    string? UpdatedAtFormatted);

public sealed record DocumentTagDetailDto(
    Guid Id,
    Guid NamespaceId,
    string? NamespaceDisplayName,
    Guid? ParentId,
    string Name,
    IReadOnlyCollection<Guid> PathIds,
    int SortOrder,
    string? Color,
    string? IconKey,
    bool IsActive,
    bool IsSystem,
    Guid? AppliedBy,
    DateTimeOffset AppliedAtUtc);

public sealed class DocumentUpdateRequest
{
    public string? Title { get; init; }

    public string? Status { get; init; }

    public string? Sensitivity { get; init; }

    public Guid? GroupId { get; init; }

    public bool HasGroupId { get; init; }
}
