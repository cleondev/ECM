using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ECM.SearchRead.Application.Search;
using ECM.SearchRead.Application.Search.Abstractions;
using Xunit;

namespace SearchRead.Tests.Application.Search;

public class SearchQueryHandlerTests
{
    [Fact]
    public async Task HandleAsync_DelegatesQueryToProvider()
    {
        var provider = new FakeSearchReadProvider();
        var handler = new SearchQueryHandler(provider);
        var query = new SearchQuery("document", Guid.NewGuid().ToString(), 10);
        var expectedResults = new[]
        {
            new SearchResult(Guid.NewGuid(), "Employee contract", 0.93),
            new SearchResult(Guid.NewGuid(), "Employee profile", 0.89)
        };
        provider.Results = expectedResults;
        var cancellationTokenSource = new CancellationTokenSource();

        var result = await handler.HandleAsync(query, cancellationTokenSource.Token);

        Assert.Equal(expectedResults, result);
        Assert.Equal(query, provider.ReceivedQuery);
        Assert.Equal(cancellationTokenSource.Token, provider.ReceivedCancellationToken);
    }

    private sealed class FakeSearchReadProvider : ISearchReadProvider
    {
        public SearchQuery? ReceivedQuery { get; private set; }

        public CancellationToken ReceivedCancellationToken { get; private set; }

        public IReadOnlyCollection<SearchResult> Results { get; set; } = [];

        public Task<IReadOnlyCollection<SearchResult>> SearchAsync(SearchQuery query, CancellationToken cancellationToken = default)
        {
            ReceivedQuery = query;
            ReceivedCancellationToken = cancellationToken;
            return Task.FromResult(Results);
        }

        public Task<IReadOnlyCollection<string>> SuggestAsync(string term, int limit, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyCollection<string>>([]);

        public Task<SearchFacetsResult> GetFacetsAsync(SearchFacetsQuery query, CancellationToken cancellationToken = default)
            => Task.FromResult(SearchFacetsResult.Empty);
    }
}
