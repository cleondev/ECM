using System;

namespace AppGateway.Contracts.Documents;

public sealed record DocumentTypeDto(
    Guid Id,
    string TypeKey,
    string TypeName,
    string? Description,
    bool IsActive,
    DateTimeOffset CreatedAtUtc);
