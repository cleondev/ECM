namespace AppGateway.Contracts.Documents;

public sealed record DocumentShareLinkDto(Uri Url, DateTimeOffset ExpiresAtUtc, bool IsPublic);
