namespace AppGateway.Contracts.IAM.Groups;

using System;

public sealed record GroupSummaryDto(Guid Id, string Name, string Kind, string Role);
