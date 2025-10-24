namespace AppGateway.Contracts.Documents;

public sealed record CreateShareLinkRequestDto(bool IsPublic, int? ExpiresInMinutes);
