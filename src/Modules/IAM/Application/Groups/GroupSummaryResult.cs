namespace ECM.IAM.Application.Groups;

using System;
using ECM.IAM.Domain.Groups;

public sealed record GroupSummaryResult(Guid Id, string Name, GroupKind Kind, string Role, Guid? ParentGroupId);
