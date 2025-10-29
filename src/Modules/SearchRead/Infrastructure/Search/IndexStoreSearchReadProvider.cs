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

    public async Task<IReadOnlyCollection<string>> SuggestAsync(
        string term,
        int limit,
        CancellationToken cancellationToken = default)
    {
        var records = await _reader.ListAsync(cancellationToken).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(term))
        {
            return records
                .Select(record => record.Title)
                .Where(title => !string.IsNullOrWhiteSpace(title))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(limit)
                .ToArray();
        }

        var comparison = StringComparison.OrdinalIgnoreCase;
        var normalizedTerm = term.Trim();

        var suggestions = records
            .Select(record => record.Title)
            .Where(title => !string.IsNullOrWhiteSpace(title) && title.Contains(normalizedTerm, comparison))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(limit)
            .ToArray();

        return suggestions;
    }

    public async Task<SearchFacetsResult> GetFacetsAsync(
        SearchFacetsQuery query,
        CancellationToken cancellationToken = default)
    {
        var records = await _reader.ListAsync(cancellationToken).ConfigureAwait(false);
        var filtered = records.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(query.Term))
        {
            filtered = filtered
                .Where(record => record.Title.Contains(query.Term, StringComparison.OrdinalIgnoreCase) || record.Content.Contains(query.Term, StringComparison.OrdinalIgnoreCase));
        }

        var facets = new Dictionary<string, Dictionary<string, int>>(StringComparer.OrdinalIgnoreCase);

        foreach (var record in filtered)
        {
            if (!string.IsNullOrWhiteSpace(query.GroupId)
                && !MetadataContainsValue(record.Metadata, "groupIds", query.GroupId))
            {
                continue;
            }

            AddFacetValue(facets, "tags", record.Tags);
            AddFacetValue(facets, "indexingType", record.IndexingType.ToString());

            foreach (var (key, value) in record.Metadata)
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    continue;
                }

                if (string.Equals(key, "groupIds", StringComparison.OrdinalIgnoreCase))
                {
                    foreach (var group in SplitMetadataValues(value))
                    {
                        AddFacetValue(facets, $"metadata:{key}", group);
                    }

                    continue;
                }

                AddFacetValue(facets, $"metadata:{key}", value);
            }
        }

        var result = facets.ToDictionary(
            pair => pair.Key,
            pair => (IReadOnlyDictionary<string, int>)pair.Value,
            StringComparer.OrdinalIgnoreCase);

        return result.Count == 0
            ? SearchFacetsResult.Empty
            : new SearchFacetsResult(result);
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

    private static bool MetadataContainsValue(
        IDictionary<string, string> metadata,
        string key,
        string value)
    {
        if (!metadata.TryGetValue(key, out var raw) || string.IsNullOrWhiteSpace(raw))
        {
            return false;
        }

        return SplitMetadataValues(raw)
            .Any(item => item.Equals(value, StringComparison.OrdinalIgnoreCase));
    }

    private static IEnumerable<string> SplitMetadataValues(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return Array.Empty<string>();
        }

        return raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    private static void AddFacetValue(
        IDictionary<string, Dictionary<string, int>> facets,
        string name,
        IEnumerable<string> values)
    {
        foreach (var value in values)
        {
            AddFacetValue(facets, name, value);
        }
    }

    private static void AddFacetValue(
        IDictionary<string, Dictionary<string, int>> facets,
        string name,
        string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        if (!facets.TryGetValue(name, out var bucket))
        {
            bucket = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            facets[name] = bucket;
        }

        bucket[value] = bucket.TryGetValue(value, out var count) ? count + 1 : 1;
    }
}
