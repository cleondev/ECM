using System;

namespace AppGateway.Contracts.Tags;

public sealed record AssignTagRequestDto(
    Guid TagId,
    Guid? AppliedBy);
