namespace ECM.IAM.Application.Users.Queries;

public sealed record AuthenticateUserQuery(string Email, string Password);
