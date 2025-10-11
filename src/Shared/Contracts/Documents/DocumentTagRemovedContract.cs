namespace Shared.Contracts.Documents;

public sealed record DocumentTagRemovedContract(
    Guid DocumentId,
    Guid TagId,
    DateTimeOffset RemovedAtUtc);
