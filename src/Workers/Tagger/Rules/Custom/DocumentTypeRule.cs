using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;

using Ecm.Rules.Abstractions;

using Tagger.Events;
using Tagger.Rules.Configuration;

namespace Tagger.Rules.Custom;

/// <summary>
/// Assigns tag names based on document file extensions, falling back to metadata when needed.
/// </summary>
internal sealed class DocumentTypeRule : IRule
{
    private static readonly IReadOnlyList<string> DocumentTypePathSegments = new[]
    {
        "LOS",
        "CreditApplication",
        "Document Types",
    };

    private static readonly TagDefinition DocumentTagDefinition = TagDefinition.Create(
        "Document",
        DocumentTypePathSegments,
        scope: TagScope.Group,
        namespaceDisplayName: TagDefaults.DefaultNamespaceDisplayName,
        color: "#4A5568",
        iconKey: "file");

    private static readonly TagDefinition ImageTagDefinition = TagDefinition.Create(
        "Images",
        DocumentTypePathSegments,
        scope: TagScope.Group,
        namespaceDisplayName: TagDefaults.DefaultNamespaceDisplayName,
        color: "#3182CE",
        iconKey: "image");

    private static readonly IReadOnlyDictionary<string, TagDefinition> ExtensionMappings =
        new Dictionary<string, TagDefinition>(StringComparer.OrdinalIgnoreCase)
        {
            [".doc"] = DocumentTagDefinition,
            [".docx"] = DocumentTagDefinition,
            [".pdf"] = DocumentTagDefinition,
            [".xls"] = DocumentTagDefinition,
            [".xlsx"] = DocumentTagDefinition,
            [".ppt"] = DocumentTagDefinition,
            [".pptx"] = DocumentTagDefinition,
            [".txt"] = DocumentTagDefinition,
            [".csv"] = DocumentTagDefinition,
            [".jpg"] = ImageTagDefinition,
            [".jpeg"] = ImageTagDefinition,
            [".png"] = ImageTagDefinition,
            [".gif"] = ImageTagDefinition,
            [".bmp"] = ImageTagDefinition,
            [".tiff"] = ImageTagDefinition,
            [".svg"] = ImageTagDefinition,
            [".heic"] = ImageTagDefinition,
        };

    private static readonly string[] MetadataExtensionKeys =
    {
        "fileExtension",
        "extension",
        "ext",
    };

    public string Name => "Document Type";

    /// <summary>
    /// Resolves an extension from integration event metadata or title for use in rule context enrichment.
    /// </summary>
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

    /// <summary>
    /// Matches when an extension can be derived from the rule context.
    /// </summary>
    public bool Match(IRuleContext ctx) => TryResolveExtension(ctx, out _);

    /// <summary>
    /// Emits a tag definition mapped from the resolved extension when available.
    /// </summary>
    public void Apply(IRuleContext ctx, IRuleOutput output)
    {
        if (!TryResolveExtension(ctx, out var extension))
        {
            return;
        }

        if (!ExtensionMappings.TryGetValue(extension, out var tagDefinition))
        {
            return;
        }

        output.AddTag(tagDefinition);
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
