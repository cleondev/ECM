using System;

namespace ECM.Document.Api.Tags.Requests;

public sealed record UpdateTagNamespaceRequest(
    string? DisplayName,
    Guid? UpdatedBy);
