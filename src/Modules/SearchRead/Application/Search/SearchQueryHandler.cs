using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ECM.SearchRead.Application.Search.Abstractions;

namespace ECM.SearchRead.Application.Search;

public sealed class SearchQueryHandler(ISearchReadProvider provider)
{
    private readonly ISearchReadProvider _provider = provider;

    public Task<IReadOnlyCollection<SearchResult>> HandleAsync(SearchQuery query, CancellationToken cancellationToken)
        => _provider.SearchAsync(query, cancellationToken);
}
