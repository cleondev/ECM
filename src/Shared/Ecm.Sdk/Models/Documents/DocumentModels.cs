namespace Ecm.Sdk;

/// <summary>
/// Represents a document returned by the ECM APIs.
/// </summary>
/// <param name="Id">Document identifier.</param>
/// <param name="Title">Document title.</param>
/// <param name="DocType">Type key defining document schema.</param>
/// <param name="Status">Workflow status of the document.</param>
/// <param name="Sensitivity">Sensitivity label applied to the document.</param>
/// <param name="OwnerId">Owner of the document.</param>
/// <param name="CreatedBy">User who created the document.</param>
/// <param name="GroupId">Primary group associated with the document.</param>
/// <param name="GroupIds">All groups associated with the document.</param>
/// <param name="CreatedAtUtc">Timestamp when the document was created.</param>
/// <param name="UpdatedAtUtc">Timestamp when the document was last updated.</param>
/// <param name="DocumentTypeId">Optional document type identifier.</param>
/// <param name="LatestVersion">Latest version details.</param>
/// <param name="Tags">Tags applied to the document.</param>
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

/// <summary>
/// Describes a single document version within ECM.
/// </summary>
/// <param name="Id">Version identifier.</param>
/// <param name="VersionNo">Sequential version number.</param>
/// <param name="StorageKey">Key used to locate the binary in storage.</param>
/// <param name="Bytes">Size of the stored binary.</param>
/// <param name="MimeType">MIME type associated with the version.</param>
/// <param name="Sha256">Hash of the stored content.</param>
/// <param name="CreatedBy">User who uploaded the version.</param>
/// <param name="CreatedAtUtc">Timestamp when the version was created.</param>
public sealed record DocumentVersionDto(
    Guid Id,
    int VersionNo,
    string StorageKey,
    long Bytes,
    string MimeType,
    string Sha256,
    Guid CreatedBy,
    DateTimeOffset CreatedAtUtc);

/// <summary>
/// Represents a lightweight tag associated with a document.
/// </summary>
/// <param name="Id">Tag identifier.</param>
/// <param name="Name">Tag display name.</param>
public sealed record DocumentTagDto(Guid Id, string Name);

/// <summary>
/// Contains profile information about a user within ECM.
/// </summary>
/// <param name="Id">User identifier.</param>
/// <param name="DisplayName">User display name.</param>
/// <param name="Email">User email address.</param>
/// <param name="PrimaryGroupId">Primary group identifier when available.</param>
/// <param name="GroupIds">Groups the user belongs to.</param>
public sealed record UserProfile(
    Guid Id,
    string DisplayName,
    string Email,
    Guid? PrimaryGroupId,
    IReadOnlyCollection<Guid> GroupIds);

/// <summary>
/// Encapsulates filtering and paging options for document listing operations.
/// </summary>
/// <param name="Query">Free-text search query.</param>
/// <param name="DocType">Document type filter.</param>
/// <param name="Status">Workflow status filter.</param>
/// <param name="Sensitivity">Sensitivity label filter.</param>
/// <param name="OwnerId">Owner filter.</param>
/// <param name="GroupId">Group filter.</param>
/// <param name="Page">Page number to retrieve.</param>
/// <param name="PageSize">Number of items per page.</param>
public sealed record DocumentListQuery(
    string? Query,
    string? DocType,
    string? Status,
    string? Sensitivity,
    Guid? OwnerId,
    Guid? GroupId,
    int Page,
    int PageSize);

/// <summary>
/// Contains a page of document list results.
/// </summary>
/// <param name="Page">Current page number.</param>
/// <param name="PageSize">Number of items per page.</param>
/// <param name="TotalItems">Total matching items.</param>
/// <param name="TotalPages">Total number of pages.</param>
/// <param name="Items">Documents returned for the page.</param>
public sealed record DocumentListResult(
    int Page,
    int PageSize,
    long TotalItems,
    int TotalPages,
    IReadOnlyCollection<DocumentListItem> Items);

/// <summary>
/// Represents a summarized document used in list results.
/// </summary>
/// <param name="Id">Document identifier.</param>
/// <param name="Title">Document title.</param>
/// <param name="DocType">Document type key.</param>
/// <param name="Status">Workflow status.</param>
/// <param name="Sensitivity">Sensitivity label.</param>
/// <param name="OwnerId">Owner identifier.</param>
/// <param name="CreatedBy">User who created the document.</param>
/// <param name="GroupId">Primary group identifier.</param>
/// <param name="GroupIds">Groups assigned to the document.</param>
/// <param name="CreatedAtUtc">Creation timestamp.</param>
/// <param name="UpdatedAtUtc">Last update timestamp.</param>
/// <param name="DocumentTypeId">Optional document type identifier.</param>
/// <param name="LatestVersion">Latest version metadata.</param>
/// <param name="Tags">Tags applied to the document.</param>
/// <param name="CreatedAtFormatted">Formatted creation timestamp for display.</param>
/// <param name="UpdatedAtFormatted">Formatted update timestamp for display.</param>
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

/// <summary>
/// Represents detailed tag information returned with a document.
/// </summary>
/// <param name="Id">Tag identifier.</param>
/// <param name="NamespaceId">Namespace identifier for the tag.</param>
/// <param name="NamespaceDisplayName">Display name for the tag namespace.</param>
/// <param name="ParentId">Identifier of the parent tag when applicable.</param>
/// <param name="Name">Tag name.</param>
/// <param name="PathIds">Identifiers representing the tag hierarchy.</param>
/// <param name="SortOrder">Sort order within the namespace.</param>
/// <param name="Color">Color associated with the tag.</param>
/// <param name="IconKey">Icon key associated with the tag.</param>
/// <param name="IsActive">Indicates whether the tag is active.</param>
/// <param name="IsSystem">Indicates whether the tag is system defined.</param>
/// <param name="AppliedBy">User who applied the tag.</param>
/// <param name="AppliedAtUtc">Timestamp when the tag was applied.</param>
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

/// <summary>
/// Represents mutable fields that can be updated on a document.
/// </summary>
public sealed class DocumentUpdateRequest
{
    /// <summary>
    /// Optional title to apply to the document.
    /// </summary>
    public string? Title { get; init; }

    /// <summary>
    /// Optional workflow status to apply.
    /// </summary>
    public string? Status { get; init; }

    /// <summary>
    /// Optional sensitivity label to apply.
    /// </summary>
    public string? Sensitivity { get; init; }

    /// <summary>
    /// Optional group association to apply to the document.
    /// </summary>
    public Guid? GroupId { get; init; }

    /// <summary>
    /// Indicates whether the group association should be explicitly updated.
    /// </summary>
    public bool HasGroupId { get; init; }
}
