using System;

namespace ECM.IAM.Application.Users.Queries;

public sealed record GetUserByIdQuery(Guid UserId);
