using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Tagger.Rules.Configuration;

internal static class TagScope
{
    public const string Global = "global";
    public const string Group = "group";
    public const string User = "user";
}

internal static class TagDefaults
{
    public const string DefaultNamespaceDisplayName = "LOS";
    public const string DefaultScope = TagScope.Group;

    public static readonly IReadOnlyList<string> DefaultPathSegments = new[] { "LOS", "CreditApplication" };
}

internal sealed record TagDefinition(
    string Name,
    IReadOnlyList<string> PathSegments,
    string Scope,
    Guid? OwnerGroupId,
    Guid? OwnerUserId,
    string NamespaceDisplayName,
    string? Color,
    string? IconKey)
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    public static IEqualityComparer<TagDefinition> Comparer { get; } = new ValueComparer();

    public static TagDefinition Create(
        string name,
        IEnumerable<string>? pathSegments = null,
        string? scope = null,
        Guid? ownerGroupId = null,
        Guid? ownerUserId = null,
        string? namespaceDisplayName = null,
        string? color = null,
        string? iconKey = null)
    {
        var normalizedName = name?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(normalizedName))
        {
            throw new ArgumentException("Tag name is required.", nameof(name));
        }

        return new TagDefinition(
            normalizedName,
            NormalizePath(pathSegments, null),
            NormalizeScope(scope),
            ownerGroupId,
            ownerUserId,
            NormalizeNamespace(namespaceDisplayName),
            Normalize(color),
            Normalize(iconKey));
    }

    public static bool TryCreate(object? value, out TagDefinition? definition)
    {
        definition = null;

        switch (value)
        {
            case null:
                return false;
            case TagDefinition existing:
                definition = existing;
                return true;
            case string text:
                definition = Create(text);
                return true;
            case JsonElement jsonElement:
                return TryCreateFromJson(jsonElement, out definition);
            case IDictionary<string, object> dictionary:
                return TryCreateFromDictionary(dictionary, out definition);
            default:
                return TryCreateFromPayload(value, out definition);
        }
    }

    private static bool TryCreateFromJson(JsonElement jsonElement, out TagDefinition? definition)
    {
        try
        {
            var payload = jsonElement.Deserialize<TagDefinitionPayload>(SerializerOptions);
            definition = payload is null ? null : FromPayload(payload);
            return definition is not null;
        }
        catch
        {
            definition = null;
            return false;
        }
    }

    private static bool TryCreateFromDictionary(IDictionary<string, object> dictionary, out TagDefinition? definition)
    {
        try
        {
            var json = JsonSerializer.Serialize(dictionary);
            var payload = JsonSerializer.Deserialize<TagDefinitionPayload>(json, SerializerOptions);
            definition = payload is null ? null : FromPayload(payload);
            return definition is not null;
        }
        catch
        {
            definition = null;
            return false;
        }
    }

    private static bool TryCreateFromPayload(object value, out TagDefinition? definition)
    {
        definition = null;

        try
        {
            var payloadJson = JsonSerializer.Serialize(value);
            var payload = JsonSerializer.Deserialize<TagDefinitionPayload>(payloadJson, SerializerOptions);
            definition = payload is null ? null : FromPayload(payload);
            return definition is not null;
        }
        catch
        {
            return false;
        }
    }

    private static TagDefinition? FromPayload(TagDefinitionPayload payload)
    {
        if (payload is null)
        {
            return null;
        }

        var name = payload.Name?.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        var scope = NormalizeScope(payload.Scope);
        var namespaceDisplayName = NormalizeNamespace(payload.NamespaceDisplayName ?? payload.Namespace);
        var color = Normalize(payload.Color);
        var iconKey = Normalize(payload.IconKey);
        var pathSegments = NormalizePath(payload.PathSegments, payload.Path);

        return new TagDefinition(
            name,
            pathSegments,
            scope,
            payload.OwnerGroupId,
            payload.OwnerUserId,
            namespaceDisplayName,
            color,
            iconKey);
    }

    private static string NormalizeScope(string? scope)
    {
        if (string.IsNullOrWhiteSpace(scope))
        {
            return TagDefaults.DefaultScope;
        }

        return scope.Trim().ToLowerInvariant() switch
        {
            TagScope.Global => TagScope.Global,
            TagScope.User => TagScope.User,
            _ => TagScope.Group
        };
    }

    private static string NormalizeNamespace(string? name)
    {
        return string.IsNullOrWhiteSpace(name)
            ? TagDefaults.DefaultNamespaceDisplayName
            : name.Trim();
    }

    private static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static IReadOnlyList<string> NormalizePath(IEnumerable<string>? segments, object? rawPath)
    {
        var normalized = new List<string>();

        if (segments is not null)
        {
            foreach (var segment in segments)
            {
                AddIfPresent(normalized, segment);
            }
        }

        if (rawPath is not null)
        {
            foreach (var segment in ExtractSegments(rawPath))
            {
                AddIfPresent(normalized, segment);
            }
        }

        return normalized.Count == 0
            ? TagDefaults.DefaultPathSegments
            : normalized;
    }

    private static IEnumerable<string> ExtractSegments(object value)
    {
        switch (value)
        {
            case string text:
                return SplitPath(text);
            case IEnumerable<string> textSegments:
                return textSegments;
            case IEnumerable<object> objectSegments:
                return objectSegments
                    .SelectMany(ExtractSegments)
                    .Where(segment => !string.IsNullOrWhiteSpace(segment));
            case JsonElement jsonElement:
                return ExtractJsonSegments(jsonElement);
            default:
                return Array.Empty<string>();
        }
    }

    private static IEnumerable<string> ExtractJsonSegments(JsonElement jsonElement)
    {
        return jsonElement.ValueKind switch
        {
            JsonValueKind.Array => jsonElement.EnumerateArray().SelectMany(ExtractJsonSegments),
            JsonValueKind.String => SplitPath(jsonElement.GetString()),
            _ => Array.Empty<string>()
        };
    }

    private static IEnumerable<string> SplitPath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return Array.Empty<string>();
        }

        return path
            .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(segment => !string.IsNullOrWhiteSpace(segment));
    }

    private static void AddIfPresent(ICollection<string> segments, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        segments.Add(value.Trim());
    }

    private sealed class ValueComparer : IEqualityComparer<TagDefinition>
    {
        public bool Equals(TagDefinition? x, TagDefinition? y)
        {
            if (ReferenceEquals(x, y))
            {
                return true;
            }

            if (x is null || y is null)
            {
                return false;
            }

            return string.Equals(x.Name, y.Name, StringComparison.OrdinalIgnoreCase)
                   && string.Equals(x.Scope, y.Scope, StringComparison.OrdinalIgnoreCase)
                   && string.Equals(x.NamespaceDisplayName, y.NamespaceDisplayName, StringComparison.OrdinalIgnoreCase)
                   && x.OwnerGroupId == y.OwnerGroupId
                   && x.OwnerUserId == y.OwnerUserId
                   && string.Equals(x.Color, y.Color, StringComparison.OrdinalIgnoreCase)
                   && string.Equals(x.IconKey, y.IconKey, StringComparison.OrdinalIgnoreCase)
                   && x.PathSegments.SequenceEqual(y.PathSegments, StringComparer.OrdinalIgnoreCase);
        }

        public int GetHashCode(TagDefinition obj)
        {
            var hash = new HashCode();

            hash.Add(obj.Name, StringComparer.OrdinalIgnoreCase);
            hash.Add(obj.Scope, StringComparer.OrdinalIgnoreCase);
            hash.Add(obj.NamespaceDisplayName, StringComparer.OrdinalIgnoreCase);
            hash.Add(obj.OwnerGroupId);
            hash.Add(obj.OwnerUserId);
            hash.Add(obj.Color, StringComparer.OrdinalIgnoreCase);
            hash.Add(obj.IconKey, StringComparer.OrdinalIgnoreCase);

            foreach (var segment in obj.PathSegments)
            {
                hash.Add(segment, StringComparer.OrdinalIgnoreCase);
            }

            return hash.ToHashCode();
        }
    }

    private sealed class TagDefinitionPayload
    {
        public string? Name { get; init; }

        public string? Scope { get; init; }

        public Guid? OwnerGroupId { get; init; }

        public Guid? OwnerUserId { get; init; }

        public string? Namespace { get; init; }

        public string? NamespaceDisplayName { get; init; }

        public IEnumerable<string>? PathSegments { get; init; }

        public object? Path { get; init; }

        public string? Color { get; init; }

        public string? IconKey { get; init; }
    }
}
