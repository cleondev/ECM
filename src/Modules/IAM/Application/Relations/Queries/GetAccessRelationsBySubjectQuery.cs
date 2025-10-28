using System;

namespace ECM.IAM.Application.Relations.Queries;

public sealed record GetAccessRelationsBySubjectQuery(string SubjectType, Guid SubjectId);
