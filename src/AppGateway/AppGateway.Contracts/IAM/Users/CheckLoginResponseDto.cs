namespace AppGateway.Contracts.IAM.Users;

public sealed record CheckLoginResponseDto(
    bool IsAuthenticated,
    string RedirectPath,
    string? LoginUrl,
    string? SilentLoginUrl,
    UserSummaryDto? Profile);
