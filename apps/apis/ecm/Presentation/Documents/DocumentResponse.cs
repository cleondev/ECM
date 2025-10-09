namespace Ecm.Presentation.Documents;

public sealed record DocumentResponse(Guid Id, string Title, DateTimeOffset CreatedAtUtc);
