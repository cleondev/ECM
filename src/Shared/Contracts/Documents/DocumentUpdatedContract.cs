using System;

namespace Shared.Contracts.Documents;

public sealed record DocumentUpdatedContract(
    Guid DocumentId,
    string Title,
    string Status,
    string Sensitivity,
    Guid? GroupId,
    Guid UpdatedBy,
    DateTimeOffset UpdatedAtUtc);
