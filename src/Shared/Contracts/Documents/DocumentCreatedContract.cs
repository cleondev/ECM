using System;

namespace Shared.Contracts.Documents;

public sealed record DocumentCreatedContract(Guid DocumentId, string Title, DateTimeOffset CreatedAtUtc);
