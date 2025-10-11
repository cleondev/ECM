using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ECM.SearchRead.Application.Search.Abstractions;

public interface ISearchReadProvider
{
    Task<IReadOnlyCollection<SearchResult>> SearchAsync(SearchQuery query, CancellationToken cancellationToken = default);
}
