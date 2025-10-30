using System;

namespace Shared.Contracts.Documents;

public sealed record DocumentCreatedContract(
    Guid DocumentId,
    string Title,
    Guid OwnerId,
    Guid CreatedBy,
    DateTimeOffset CreatedAtUtc);
