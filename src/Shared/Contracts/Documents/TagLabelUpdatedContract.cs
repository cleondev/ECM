namespace Shared.Contracts.Documents;

public sealed record TagLabelUpdatedContract(
    Guid TagId,
    string NamespaceSlug,
    string Path,
    Guid? UpdatedBy,
    DateTimeOffset UpdatedAtUtc);
