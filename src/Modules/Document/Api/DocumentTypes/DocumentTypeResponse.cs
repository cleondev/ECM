using System;

namespace ECM.Document.Api.DocumentTypes;

public sealed record DocumentTypeResponse(
    Guid Id,
    string TypeKey,
    string TypeName,
    bool IsActive,
    DateTimeOffset CreatedAtUtc);
