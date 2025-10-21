namespace AppGateway.Contracts.IAM.Users;

public sealed record CheckLoginResponseDto(bool IsAuthenticated, string? LoginUrl, UserSummaryDto? Profile);
