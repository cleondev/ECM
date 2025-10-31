using System;
using System.Collections.Generic;
using System.Text.Json;

using Microsoft.Extensions.Primitives;

namespace Shared.Extensions.Primitives;

public static class StringValuesExtensions
{
    public static GuidCollectionParseResult ParseGuidValues(this StringValues values)
    {
        if (values.Count == 0)
        {
            return GuidCollectionParseResult.Empty;
        }

        var buffer = new List<Guid>();
        var invalid = new List<string>();
        var seen = new HashSet<Guid>();

        void Add(Guid value)
        {
            if (value == Guid.Empty)
            {
                return;
            }

            if (seen.Add(value))
            {
                buffer.Add(value);
            }
        }

        bool TryParseValue(string? candidate)
        {
            if (string.IsNullOrWhiteSpace(candidate))
            {
                return false;
            }

            if (Guid.TryParse(candidate, out var guid))
            {
                Add(guid);
                return true;
            }

            invalid.Add(candidate);
            return false;
        }

        if (values.Count > 1)
        {
            foreach (var candidate in values)
            {
                TryParseValue(candidate);
            }

            return new GuidCollectionParseResult(buffer, invalid);
        }

        var raw = values[0];
        if (string.IsNullOrWhiteSpace(raw))
        {
            return GuidCollectionParseResult.Empty;
        }

        var trimmed = raw.Trim();
        if (trimmed.StartsWith('['))
        {
            try
            {
                var parsed = JsonSerializer.Deserialize<string[]>(trimmed);
                if (parsed is not null)
                {
                    foreach (var candidate in parsed)
                    {
                        TryParseValue(candidate);
                    }

                    return new GuidCollectionParseResult(buffer, invalid);
                }
            }
            catch (JsonException)
            {
                // fall through to delimiter parsing
            }
        }

        foreach (var segment in trimmed.Split([',', ';'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            TryParseValue(segment);
        }

        return new GuidCollectionParseResult(buffer, invalid);
    }

    public readonly record struct GuidCollectionParseResult(IReadOnlyList<Guid> Values, IReadOnlyList<string> InvalidValues)
    {
        public static GuidCollectionParseResult Empty { get; } = new([], []);

        public bool HasValues => Values.Count > 0;

        public bool HasInvalidValues => InvalidValues.Count > 0;
    }
}
