using System.Threading;
using System.Threading.Tasks;
using ECM.SearchRead.Application.Search.Abstractions;

namespace ECM.SearchRead.Application.Search;

public sealed class SearchFacetsQueryHandler(ISearchReadProvider provider)
{
    private readonly ISearchReadProvider _provider = provider;

    public Task<SearchFacetsResult> HandleAsync(SearchFacetsQuery query, CancellationToken cancellationToken)
        => _provider.GetFacetsAsync(query, cancellationToken);
}
