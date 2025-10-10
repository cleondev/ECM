using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ECM.SearchRead.Application.Search;

namespace ECM.SearchRead.Infrastructure.Search;

public interface ISearchReadProvider
{
    Task<IReadOnlyCollection<SearchResult>> SearchAsync(SearchQuery query, CancellationToken cancellationToken = default);
}
