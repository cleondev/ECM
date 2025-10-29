namespace ECM.IAM.Api.Groups;

using System;

public sealed record GroupResponse(Guid Id, string Name, string Kind, string Role, Guid? ParentGroupId);
