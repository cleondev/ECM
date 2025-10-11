namespace Shared.Contracts.Documents;

public sealed record TagLabelCreatedContract(
    Guid TagId,
    string NamespaceSlug,
    string Path,
    Guid? CreatedBy,
    DateTimeOffset CreatedAtUtc);
