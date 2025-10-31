using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

using Shared.Extensions.Primitives;

namespace AppGateway.Api.Controllers.Documents;

internal static class DocumentFormFieldReader
{
    private static readonly string[] MetadataPrefixes = ["meta", "Meta"];
    private static readonly char[] FieldDelimiters = ['[', ']', '.'];

    public static string? GetString(IFormCollection form, string propertyName)
    {
        foreach (var field in EnumerateFieldNames(propertyName))
        {
            if (form.TryGetValue(field, out var value) && !StringValues.IsNullOrEmpty(value))
            {
                return value.ToString();
            }
        }

        return null;
    }

    public static Guid? GetGuid(IFormCollection form, string propertyName)
    {
        var value = GetString(form, propertyName);
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return Guid.TryParse(value, out var parsed) ? parsed : null;
    }

    public static IReadOnlyList<Guid> GetGuidList(IFormCollection form, string propertyName)
    {
        foreach (var field in EnumerateListFieldNames(propertyName))
        {
            if (!form.TryGetValue(field, out var values) || values.Count == 0)
            {
                continue;
            }

            var parsed = values.ParseGuidValues();
            if (parsed.Values.Count > 0)
            {
                return parsed.Values;
            }
        }

        foreach (var field in EnumerateFieldNames(propertyName))
        {
            if (!form.TryGetValue(field, out var values) || values.Count == 0)
            {
                continue;
            }

            var parsed = values.ParseGuidValues();
            if (parsed.Values.Count > 0)
            {
                return parsed.Values;
            }
        }

        var buffer = new List<Guid>();
        var seen = new HashSet<Guid>();

        foreach (var key in form.Keys)
        {
            if (!IsListFieldName(key, propertyName))
            {
                continue;
            }

            if (!form.TryGetValue(key, out var values) || values.Count == 0)
            {
                continue;
            }

            var parsed = values.ParseGuidValues();
            if (parsed.Values.Count == 0)
            {
                continue;
            }

            foreach (var value in parsed.Values)
            {
                if (seen.Add(value))
                {
                    buffer.Add(value);
                }
            }
        }

        if (buffer.Count > 0)
        {
            return buffer;
        }

        return [];
    }

    public static IFormFile? GetFile(IFormCollection form, string propertyName)
    {
        if (form.Files.Count == 0)
        {
            return null;
        }

        var file = form.Files[propertyName];
        if (file is not null)
        {
            return file;
        }

        return form.Files.Count > 0 ? form.Files[0] : null;
    }

    public static IReadOnlyList<IFormFile> GetFiles(IFormCollection form)
    {
        if (form.Files.Count == 0)
        {
            return [];
        }

        var files = form.Files.GetFiles("Files");
        if (files.Count > 0)
        {
            return files.ToList();
        }

        files = form.Files.GetFiles("files");
        if (files.Count > 0)
        {
            return files.ToList();
        }

        return form.Files.ToList();
    }

    private static IEnumerable<string> EnumerateFieldNames(string propertyName)
    {
        var camelCase = char.ToLowerInvariant(propertyName[0]) + propertyName[1..];

        yield return propertyName;
        yield return camelCase;

        foreach (var prefix in MetadataPrefixes)
        {
            yield return $"{prefix}[{propertyName}]";
            yield return $"{prefix}[{camelCase}]";
            yield return $"{prefix}.{propertyName}";
            yield return $"{prefix}.{camelCase}";
        }

    }

    private static IEnumerable<string> EnumerateListFieldNames(string propertyName)
    {
        var camelCase = char.ToLowerInvariant(propertyName[0]) + propertyName[1..];

        yield return $"{propertyName}[]";
        yield return $"{camelCase}[]";

        foreach (var prefix in MetadataPrefixes)
        {
            yield return $"{prefix}[{propertyName}][]";
            yield return $"{prefix}[{camelCase}][]";
            yield return $"{prefix}.{propertyName}[]";
            yield return $"{prefix}.{camelCase}[]";
        }
    }

    private static bool IsListFieldName(string key, string propertyName)
    {
        if (!IsPotentialMatch(key, propertyName))
        {
            return false;
        }

        return key.Contains('[');
    }

    private static bool IsPotentialMatch(string key, string propertyName)
    {
        if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(propertyName))
        {
            return false;
        }

        var segments = key.Split(FieldDelimiters, StringSplitOptions.RemoveEmptyEntries);
        foreach (var segment in segments)
        {
            if (string.Equals(segment, propertyName, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
