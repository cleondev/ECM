using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ECM.SearchRead.Application.Search;

namespace ECM.SearchRead.Infrastructure.Search;

internal sealed class InMemorySearchReadProvider : ISearchReadProvider
{
    private static readonly IReadOnlyCollection<SearchResult> Seed = new[]
    {
        new SearchResult(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), "Sample Contract", 0.95),
        new SearchResult(Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"), "Invoice #1001", 0.89)
    };

    public Task<IReadOnlyCollection<SearchResult>> SearchAsync(SearchQuery query, CancellationToken cancellationToken = default)
    {
        var results = Seed
            .Where(result => result.Title.Contains(query.Term, StringComparison.OrdinalIgnoreCase))
            .Take(query.Limit)
            .ToArray();

        return Task.FromResult<IReadOnlyCollection<SearchResult>>(results);
    }
}
