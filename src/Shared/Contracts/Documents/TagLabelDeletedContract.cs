namespace Shared.Contracts.Documents;

public sealed record TagLabelDeletedContract(
    Guid TagId,
    string NamespaceSlug,
    string Path,
    DateTimeOffset DeletedAtUtc);
