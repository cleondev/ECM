using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ECM.SearchRead.Application.Search.Abstractions;

public interface ISearchReadProvider
{
    Task<IReadOnlyCollection<SearchResult>> SearchAsync(SearchQuery query, CancellationToken cancellationToken = default);

    Task<IReadOnlyCollection<string>> SuggestAsync(string term, int limit, CancellationToken cancellationToken = default);

    Task<SearchFacetsResult> GetFacetsAsync(SearchFacetsQuery query, CancellationToken cancellationToken = default);
}
