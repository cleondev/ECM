namespace Ecm.FileIntegration;

public sealed record DocumentUploadRequest(
    Guid OwnerId,
    Guid CreatedBy,
    string DocType,
    string Status,
    string Sensitivity,
    string FilePath)
{
    public Guid? DocumentTypeId { get; init; }

    public string? Title { get; init; }

    public string? ContentType { get; init; }
}

public sealed record DocumentDto(
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
    IReadOnlyCollection<DocumentTagDto> Tags);

public sealed record DocumentVersionDto(
    Guid Id,
    int VersionNo,
    string StorageKey,
    long Bytes,
    string MimeType,
    string Sha256,
    Guid CreatedBy,
    DateTimeOffset CreatedAtUtc);

public sealed record DocumentTagDto(Guid Id, string Name);

public sealed record UserProfile(
    Guid Id,
    string DisplayName,
    string Email,
    Guid? PrimaryGroupId,
    IReadOnlyCollection<Guid> GroupIds);

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
