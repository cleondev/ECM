namespace ECM.SearchRead.Application.Search;

public sealed record SearchQuery(string Term, string? GroupId, int Limit = 20);
