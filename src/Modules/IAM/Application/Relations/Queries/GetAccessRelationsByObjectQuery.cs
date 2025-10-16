using System;

namespace ECM.IAM.Application.Relations.Queries;

public sealed record GetAccessRelationsByObjectQuery(string ObjectType, Guid ObjectId);
