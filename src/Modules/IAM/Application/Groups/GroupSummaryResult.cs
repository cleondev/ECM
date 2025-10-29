namespace ECM.IAM.Application.Groups;

using System;

public sealed record GroupSummaryResult(Guid Id, string Name, string Kind, string Role);
