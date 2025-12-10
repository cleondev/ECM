using System.Diagnostics.CodeAnalysis;
using System.IO;

using Ecm.Rules.Abstractions;

using Tagger.Events;
using Tagger.Rules.Configuration;

namespace Tagger.Rules.Custom;

internal sealed class DocumentTypeRule : IRule
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

    public string Name => "Document Type";

    public static string? ResolveExtension(ITaggingIntegrationEvent integrationEvent)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        if (TryResolveFromMetadata(integrationEvent.Metadata, out var extension))
        {
            return extension;
        }

        if (!string.IsNullOrWhiteSpace(integrationEvent.Title))
        {
            var titleExtension = Path.GetExtension(integrationEvent.Title);
            if (!string.IsNullOrWhiteSpace(titleExtension))
            {
                return titleExtension.Trim();
            }
        }

        return null;
    }

    public bool Match(IRuleContext ctx) => TryResolveExtension(ctx, out _);

    public void Apply(IRuleContext ctx, IRuleOutput output)
    {
        if (!TryResolveExtension(ctx, out var extension))
        {
            return;
        }

        if (!ExtensionMappings.TryGetValue(extension, out var tagName))
        {
            return;
        }

        output.AddTagName(tagName);
    }

    private static bool TryResolveExtension(IRuleContext context, [NotNullWhen(true)] out string? extension)
    {
        extension = NormalizeExtension(context.Get("extension", default(string)));

        if (string.IsNullOrWhiteSpace(extension))
        {
            foreach (var key in MetadataExtensionKeys)
            {
                extension = NormalizeExtension(context.Get(key, default(string)));
                if (!string.IsNullOrWhiteSpace(extension))
                {
                    break;
                }
            }
        }

        return !string.IsNullOrWhiteSpace(extension);
    }

    private static bool TryResolveFromMetadata(
        IDictionary<string, string>? metadata,
        [NotNullWhen(true)] out string? extension)
    {
        extension = null;

        if (metadata is null)
        {
            return false;
        }

        foreach (var key in MetadataExtensionKeys)
        {
            if (!metadata.TryGetValue(key, out var value))
            {
                continue;
            }

            extension = NormalizeExtension(value);
            if (!string.IsNullOrWhiteSpace(extension))
            {
                return true;
            }
        }

        return false;
    }

    private static string? NormalizeExtension(string? value)
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

        return trimmed.StartsWith(".", StringComparison.Ordinal) ? trimmed : $".{trimmed}";
    }
}
