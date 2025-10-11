using System;

namespace ECM.AccessControl.Application.Users.Queries;

public sealed record GetUserByIdQuery(Guid UserId);
