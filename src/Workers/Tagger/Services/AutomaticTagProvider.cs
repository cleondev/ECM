using System;
using System.Collections.Generic;
using System.IO;

namespace Tagger;

internal static class AutomaticTagProvider
{
    private static readonly IReadOnlyDictionary<string, string> ExtensionMappings = new Dictionary<string, string>(
        StringComparer.OrdinalIgnoreCase)
    {
        [".doc"] = "Document",
        [".docx"] = "Document",
        [".pdf"] = "Document",
        [".xls"] = "Document",
        [".xlsx"] = "Document",
        [".ppt"] = "Document",
        [".pptx"] = "Document",
        [".txt"] = "Document",
        [".csv"] = "Document",
        [".jpg"] = "Images",
        [".jpeg"] = "Images",
        [".png"] = "Images",
        [".gif"] = "Images",
        [".bmp"] = "Images",
        [".tiff"] = "Images",
        [".svg"] = "Images",
        [".heic"] = "Images",
    };

    private static readonly string[] MetadataExtensionKeys =
    {
        "fileExtension",
        "extension",
        "ext",
    };

    public static IReadOnlyCollection<string> GetAutomaticTags(ITaggingIntegrationEvent integrationEvent)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        var tags = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            FormatUploadDateTag(integrationEvent.OccurredAtUtc)
        };

        var extension = ExtractExtension(integrationEvent);
        if (!string.IsNullOrWhiteSpace(extension)
            && ExtensionMappings.TryGetValue(extension!, out var mappedTag))
        {
            tags.Add(mappedTag);
        }

        return tags;
    }

    private static string FormatUploadDateTag(DateTimeOffset occurredAt)
        => $"Uploaded {occurredAt:yyyy-MM-dd}";

    private static string? ExtractExtension(ITaggingIntegrationEvent integrationEvent)
    {
        if (integrationEvent.Metadata is not null)
        {
            foreach (var key in MetadataExtensionKeys)
            {
                if (!integrationEvent.Metadata.TryGetValue(key, out var value))
                {
                    continue;
                }

                var normalized = NormalizeExtension(value);
                if (!string.IsNullOrWhiteSpace(normalized))
                {
                    return normalized;
                }
            }
        }

        if (!string.IsNullOrWhiteSpace(integrationEvent.Title))
        {
            var extension = Path.GetExtension(integrationEvent.Title);
            if (!string.IsNullOrWhiteSpace(extension))
            {
                return extension.Trim();
            }
        }

        return null;
    }

    private static string? NormalizeExtension(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var trimmed = value.Trim();

        if (trimmed.Contains(' ', StringComparison.Ordinal))
        {
            trimmed = trimmed[..trimmed.IndexOf(' ', StringComparison.Ordinal)];
        }

        if (trimmed.Contains(',', StringComparison.Ordinal))
        {
            trimmed = trimmed[..trimmed.IndexOf(',', StringComparison.Ordinal)];
        }

        return trimmed.StartsWith('.', StringComparison.Ordinal) ? trimmed : $".{trimmed}";
    }
}
