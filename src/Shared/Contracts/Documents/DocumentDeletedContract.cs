using System;

namespace Shared.Contracts.Documents;

public sealed record DocumentDeletedContract(
    Guid DocumentId,
    Guid DeletedBy,
    DateTimeOffset DeletedAtUtc);
