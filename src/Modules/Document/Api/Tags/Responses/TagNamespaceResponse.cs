using System;

namespace ECM.Document.Api.Tags.Responses;

public sealed record TagNamespaceResponse(
    Guid Id,
    string Scope,
    Guid? OwnerUserId,
    Guid? OwnerGroupId,
    string? DisplayName,
    bool IsSystem,
    DateTimeOffset CreatedAtUtc);
