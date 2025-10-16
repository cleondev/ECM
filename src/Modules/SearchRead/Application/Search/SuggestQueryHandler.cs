using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ECM.SearchRead.Application.Search.Abstractions;

namespace ECM.SearchRead.Application.Search;

public sealed class SuggestQueryHandler(ISearchReadProvider provider)
{
    private readonly ISearchReadProvider _provider = provider;

    public Task<IReadOnlyCollection<string>> HandleAsync(SuggestQuery query, CancellationToken cancellationToken)
        => _provider.SuggestAsync(query.Term, query.Limit, cancellationToken);
}
