namespace ECM.Document.Api.Documents;

public sealed record DocumentShareLinkResponse(Uri Url, DateTimeOffset ExpiresAtUtc, bool IsPublic);
