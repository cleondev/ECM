using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ECM.SearchIndexer.Application.Indexing.Abstractions;
using ECM.SearchIndexer.Domain.Indexing;
using ECM.SearchRead.Application.Search;
using ECM.SearchRead.Application.Search.Abstractions;

namespace ECM.SearchRead.Infrastructure.Search;

internal sealed class IndexStoreSearchReadProvider(ISearchIndexReader reader) : ISearchReadProvider
{
    private readonly ISearchIndexReader _reader = reader;

    public async Task<IReadOnlyCollection<SearchResult>> SearchAsync(
        SearchQuery query,
        CancellationToken cancellationToken = default)
    {
        var records = await _reader.ListAsync(cancellationToken).ConfigureAwait(false);
        var results = records
            .Select(record => new { record, Score = CalculateScore(record, query.Term) })
            .Where(item => item.Score > 0)
            .OrderByDescending(item => item.Score)
            .Take(query.Limit)
            .Select(item => new SearchResult(item.record.DocumentId, item.record.Title, item.Score))
            .ToArray();

        return results;
    }

    private static double CalculateScore(SearchIndexRecord record, string term)
    {
        if (string.IsNullOrWhiteSpace(term))
        {
            return 0;
        }

        var comparison = StringComparison.OrdinalIgnoreCase;
        var normalizedTerm = term.Trim();

        var score = 0d;
        if (record.Title.Contains(normalizedTerm, comparison))
        {
            score += 3d;
        }

        if (record.Content.Contains(normalizedTerm, comparison))
        {
            score += 1.5d;
        }

        if (record.Metadata.Values.Any(value => value.Contains(normalizedTerm, comparison)))
        {
            score += 1d;
        }

        if (record.Tags.Any(tag => tag.Contains(normalizedTerm, comparison)))
        {
            score += 1.5d;
        }

        return score;
    }
}
