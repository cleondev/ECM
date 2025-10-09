using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ECM.Modules.SearchRead.Application.Search;

namespace ECM.Modules.SearchRead.Infrastructure.Search;

public interface ISearchReadProvider
{
    Task<IReadOnlyCollection<SearchResult>> SearchAsync(SearchQuery query, CancellationToken cancellationToken = default);
}
