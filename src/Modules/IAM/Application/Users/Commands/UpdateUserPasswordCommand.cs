namespace ECM.IAM.Application.Users.Commands;

public sealed record UpdateUserPasswordCommand(
    string Email,
    string? CurrentPassword,
    string NewPassword);
