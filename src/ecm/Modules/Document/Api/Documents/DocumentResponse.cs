namespace ECM.Modules.Document.Api.Documents;

public sealed record DocumentResponse(Guid Id, string Title, DateTimeOffset CreatedAtUtc);
