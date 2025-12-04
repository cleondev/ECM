using System;

namespace AppGateway.Contracts.Tags;

public sealed record UpdateTagNamespaceRequestDto(
    string? DisplayName,
    Guid? UpdatedBy);
