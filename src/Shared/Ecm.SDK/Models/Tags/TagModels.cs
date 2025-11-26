namespace Ecm.Sdk;

/// <summary>
/// Represents a tag label available within ECM.
/// </summary>
/// <param name="Id">Tag identifier.</param>
/// <param name="NamespaceId">Namespace identifier for the tag.</param>
/// <param name="NamespaceScope">Scope of the namespace.</param>
/// <param name="NamespaceDisplayName">Display name of the namespace.</param>
/// <param name="ParentId">Parent tag identifier when applicable.</param>
/// <param name="Name">Display name of the tag.</param>
/// <param name="PathIds">Identifiers representing the hierarchical path.</param>
/// <param name="SortOrder">Ordering value within the namespace.</param>
/// <param name="Color">Color associated with the tag.</param>
/// <param name="IconKey">Icon key associated with the tag.</param>
/// <param name="IsActive">Indicates whether the tag is active.</param>
/// <param name="IsSystem">Indicates whether the tag is system defined.</param>
/// <param name="CreatedBy">User who created the tag.</param>
/// <param name="CreatedAtUtc">Timestamp when the tag was created.</param>
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

/// <summary>
/// Represents the details required to create a tag.
/// </summary>
/// <param name="NamespaceId">Namespace in which the tag will reside.</param>
/// <param name="ParentId">Optional parent tag.</param>
/// <param name="Name">Name of the new tag.</param>
/// <param name="SortOrder">Ordering value for the tag.</param>
/// <param name="Color">Color to associate with the tag.</param>
/// <param name="IconKey">Icon key for the tag.</param>
/// <param name="CreatedBy">User creating the tag.</param>
/// <param name="IsSystem">Indicates whether the tag is system defined.</param>
public sealed record TagCreateRequest(
    Guid? NamespaceId,
    Guid? ParentId,
    string Name,
    int? SortOrder,
    string? Color,
    string? IconKey,
    Guid? CreatedBy,
    bool IsSystem = false);

/// <summary>
/// Represents the details required to update a tag.
/// </summary>
/// <param name="NamespaceId">Namespace in which the tag exists.</param>
/// <param name="ParentId">Optional parent tag identifier.</param>
/// <param name="Name">Updated name for the tag.</param>
/// <param name="SortOrder">Ordering value for the tag.</param>
/// <param name="Color">Color to associate with the tag.</param>
/// <param name="IconKey">Icon key for the tag.</param>
/// <param name="IsActive">Indicates whether the tag remains active.</param>
/// <param name="UpdatedBy">User performing the update.</param>
public sealed record TagUpdateRequest(
    Guid NamespaceId,
    Guid? ParentId,
    string Name,
    int? SortOrder,
    string? Color,
    string? IconKey,
    bool IsActive,
    Guid? UpdatedBy);

/// <summary>
/// Represents the information required to assign an existing tag to a document.
/// </summary>
/// <param name="TagId">Identifier of the tag to apply.</param>
/// <param name="AppliedBy">Identifier of the user applying the tag.</param>
public sealed record AssignTagRequest(Guid TagId, Guid? AppliedBy);
