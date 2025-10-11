namespace Shared.Contracts.Documents;

public sealed record DocumentTagAssignedContract(
    Guid DocumentId,
    Guid TagId,
    Guid? AppliedBy,
    DateTimeOffset AppliedAtUtc);
