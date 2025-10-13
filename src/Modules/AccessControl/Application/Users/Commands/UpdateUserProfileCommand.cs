namespace ECM.AccessControl.Application.Users.Commands;

public sealed record UpdateUserProfileCommand(
    string Email,
    string DisplayName,
    string? Department);
