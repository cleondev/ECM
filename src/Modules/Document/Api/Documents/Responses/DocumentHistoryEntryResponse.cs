using System;

namespace ECM.Document.Api.Documents.Responses;

public sealed record DocumentHistoryEntryResponse(
    Guid Id,
    string PropertyName,
    string? OldValue,
    string? NewValue,
    Guid ChangedBy,
    DateTimeOffset ChangedAtUtc);
