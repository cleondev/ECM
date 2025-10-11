using System;

namespace ECM.AccessControl.Application.Relations.Queries;

public sealed record GetAccessRelationsByObjectQuery(string ObjectType, Guid ObjectId);
