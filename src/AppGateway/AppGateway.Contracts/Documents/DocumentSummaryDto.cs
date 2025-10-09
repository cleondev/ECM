namespace AppGateway.Contracts.Documents;

public sealed record DocumentSummaryDto(Guid Id, string Title, DateTimeOffset CreatedAtUtc);
