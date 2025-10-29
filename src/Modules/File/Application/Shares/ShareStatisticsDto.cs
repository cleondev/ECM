namespace ECM.File.Application.Shares;

public sealed record ShareStatisticsDto(
    Guid ShareId,
    long Views,
    long Downloads,
    long Failures,
    DateTimeOffset? LastAccessUtc);
