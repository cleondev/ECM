namespace ECM.Modules.Document.Application.Documents;

public sealed record DocumentSummary(Guid Id, string Title, DateTimeOffset CreatedAtUtc);
