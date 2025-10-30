using System;

namespace Shared.Contracts.Documents;

public sealed record TagLabelDeletedContract(
    Guid TagId,
    Guid NamespaceId,
    DateTimeOffset DeletedAtUtc);
