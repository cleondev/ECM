using System;
using System.Collections.Generic;
using System.Globalization;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace Shared.Extensions.Http;

public static class QueryCollectionExtensions
{
    public static bool TryGetValue<T>(this IQueryCollection query, string key, out T value, IFormatProvider? formatProvider = null)
        where T : IParsable<T>
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        var raw = GetFirstValue(query, key);
        if (raw is null)
        {
            value = default!;
            return false;
        }

        var provider = formatProvider ?? CultureInfo.InvariantCulture;
        if (!T.TryParse(raw, provider, out var parsedValue))
        {
            value = default!;
            return false;
        }

        value = parsedValue!;
        return true;
    }

    public static bool TryGetValue<T>(this IQueryCollection query, IEnumerable<string> keys, out T value, IFormatProvider? formatProvider = null)
        where T : IParsable<T>
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(keys);

        foreach (var key in keys)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                continue;
            }

            if (query.TryGetValue(key, out value, formatProvider))
            {
                return true;
            }
        }

        value = default!;
        return false;
    }

    public static T? GetValue<T>(this IQueryCollection query, string key, IFormatProvider? formatProvider = null)
        where T : IParsable<T>
    {
        return query.TryGetValue<T>(key, out var value, formatProvider) ? value : default;
    }

    public static T? GetValue<T>(this IQueryCollection query, IEnumerable<string> keys, IFormatProvider? formatProvider = null)
        where T : IParsable<T>
    {
        return query.TryGetValue(keys, out T value, formatProvider) ? value : default;
    }

    public static string? GetValue(this IQueryCollection query, string key)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        return GetFirstValue(query, key);
    }

    public static string? GetValue(this IQueryCollection query, IEnumerable<string> keys)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(keys);

        foreach (var key in keys)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                continue;
            }

            var value = GetFirstValue(query, key);
            if (value is not null)
            {
                return value;
            }
        }

        return null;
    }

    public static T? GetValue<T>(this IQueryCollection query, string key, Func<string, T?> converter)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);
        ArgumentNullException.ThrowIfNull(converter);

        var value = GetFirstValue(query, key);
        return value is null ? default : converter(value);
    }

    public static int? GetInt32(this IQueryCollection query, string key)
        => query.GetValue<int>(key);

    public static string? GetString(this IQueryCollection query, string key)
        => query.GetValue(key, static value => value);

    public static bool TryGetString(this IQueryCollection query, IEnumerable<string> keys, out string value)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(keys);

        foreach (var key in keys)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                continue;
            }

            if (!query.TryGetValue(key, out var values))
            {
                continue;
            }

            var candidate = GetFirstValue(values);
            if (candidate is null)
            {
                continue;
            }

            value = candidate;
            return true;
        }

        value = default!;
        return false;
    }

    public static bool TryGetValues(this IQueryCollection query, IEnumerable<string> keys, out StringValues values)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(keys);

        foreach (var key in keys)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                continue;
            }

            if (query.TryGetValue(key, out values))
            {
                return true;
            }
        }

        values = default;
        return false;
    }

    private static string? GetFirstValue(IQueryCollection query, string key)
    {
        if (!query.TryGetValue(key, out var values))
        {
            return null;
        }

        return GetFirstValue(values);
    }

    private static string? GetFirstValue(StringValues values)
    {
        for (var index = 0; index < values.Count; index++)
        {
            var value = values[index];
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value.Trim();
            }
        }

        return null;
    }
}
