namespace Shared.Contracts.AccessControl;

public sealed record UserCreatedContract(
    Guid UserId,
    string Email,
    string DisplayName,
    string? Department,
    bool IsActive,
    DateTimeOffset CreatedAtUtc);
