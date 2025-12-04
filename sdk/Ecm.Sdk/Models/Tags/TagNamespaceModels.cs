namespace Ecm.Sdk.Models.Tags;

/// <summary>
/// Represents a tag namespace.
/// </summary>
/// <param name="Id">Namespace identifier.</param>
/// <param name="Scope">Scope for the namespace.</param>
/// <param name="OwnerUserId">User owner when scoped to a user.</param>
/// <param name="OwnerGroupId">Group owner when scoped to a group.</param>
/// <param name="DisplayName">Display name of the namespace.</param>
/// <param name="IsSystem">Indicates whether the namespace is system defined.</param>
/// <param name="CreatedAtUtc">Creation timestamp.</param>
public sealed record TagNamespaceDto(
    Guid Id,
    string Scope,
    Guid? OwnerUserId,
    Guid? OwnerGroupId,
    string? DisplayName,
    bool IsSystem,
    DateTimeOffset CreatedAtUtc);

/// <summary>
/// Represents the details required to create a tag namespace.
/// </summary>
/// <param name="Scope">Scope for the namespace.</param>
/// <param name="DisplayName">Display name of the namespace.</param>
/// <param name="OwnerGroupId">Group owner when scoped to group.</param>
/// <param name="OwnerUserId">User owner when scoped to user.</param>
/// <param name="CreatedBy">User creating the namespace.</param>
public sealed record TagNamespaceCreateRequest(
    string Scope,
    string? DisplayName,
    Guid? OwnerGroupId,
    Guid? OwnerUserId,
    Guid? CreatedBy);

/// <summary>
/// Represents the details required to update a tag namespace.
/// </summary>
/// <param name="DisplayName">New display name for the namespace.</param>
/// <param name="UpdatedBy">User updating the namespace.</param>
public sealed record TagNamespaceUpdateRequest(
    string? DisplayName,
    Guid? UpdatedBy);
