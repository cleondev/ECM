namespace Shared.Contracts.IAM;

public sealed record UserCreatedContract(
    Guid UserId,
    string Email,
    string DisplayName,
    bool IsActive,
    DateTimeOffset CreatedAtUtc);
