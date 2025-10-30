using System.Collections.Generic;

namespace ECM.SearchRead.Application.Search;

public sealed record SearchFacetsResult(IReadOnlyDictionary<string, IReadOnlyDictionary<string, int>> Facets)
{
    public static readonly SearchFacetsResult Empty = new(new Dictionary<string, IReadOnlyDictionary<string, int>>());
}
