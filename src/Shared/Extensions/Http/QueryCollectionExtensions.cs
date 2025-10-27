using System;
using System.Globalization;

using Microsoft.AspNetCore.Http;

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

    public static T? GetValue<T>(this IQueryCollection query, string key, IFormatProvider? formatProvider = null)
        where T : IParsable<T>
    {
        return query.TryGetValue<T>(key, out var value, formatProvider) ? value : default;
    }

    public static string? GetValue(this IQueryCollection query, string key)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        return GetFirstValue(query, key);
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

    private static string? GetFirstValue(IQueryCollection query, string key)
    {
        if (!query.TryGetValue(key, out var values))
        {
            return null;
        }

        for (var index = 0; index < values.Count; index++)
        {
            var value = values[index];
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return null;
    }
}
