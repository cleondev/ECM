namespace ECM.Document.Domain.Shares;

public sealed record ShareStatistics(
    Guid ShareId,
    long Views,
    long Downloads,
    long Failures,
    DateTimeOffset? LastAccessUtc);
