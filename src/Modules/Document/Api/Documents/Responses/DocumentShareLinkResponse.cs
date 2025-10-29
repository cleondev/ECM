namespace ECM.Document.Api.Documents.Responses;

public sealed record DocumentShareLinkResponse(Uri Url, DateTimeOffset ExpiresAtUtc, bool IsPublic);
