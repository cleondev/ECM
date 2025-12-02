using System;
using System.Text.Json;

namespace ECM.Document.Api.DocumentTypes;

public sealed record DocumentTypeResponse(
    Guid Id,
    string TypeKey,
    string TypeName,
    string? Description,
    bool IsActive,
    DateTimeOffset CreatedAtUtc,
    JsonDocument Config);
