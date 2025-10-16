namespace ECM.SearchRead.Application.Search;

public sealed record SuggestQuery(string Term, int Limit = 5);
