using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ECM.SearchRead.Infrastructure.Search;

namespace ECM.SearchRead.Application.Search;

public sealed class SearchApplicationService(ISearchReadProvider provider)
{
    private readonly ISearchReadProvider _provider = provider;

    public Task<IReadOnlyCollection<SearchResult>> SearchAsync(SearchQuery query, CancellationToken cancellationToken)
        => _provider.SearchAsync(query, cancellationToken);
}
