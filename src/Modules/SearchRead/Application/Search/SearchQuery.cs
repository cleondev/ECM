namespace ECM.SearchRead.Application.Search;

public sealed record SearchQuery(string Term, string? Department, int Limit = 20);
