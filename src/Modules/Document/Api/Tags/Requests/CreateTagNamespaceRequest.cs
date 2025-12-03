using System;

namespace ECM.Document.Api.Tags.Requests;

public sealed record CreateTagNamespaceRequest(
    string Scope,
    string? DisplayName,
    Guid? OwnerGroupId,
    Guid? OwnerUserId,
    Guid? CreatedBy);
