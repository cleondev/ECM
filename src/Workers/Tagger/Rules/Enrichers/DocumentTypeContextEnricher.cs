using System.Collections.Generic;
using System.IO;
using System.Threading;

using Ecm.Sdk.Clients;

using Microsoft.Extensions.Logging;

using Tagger.Events;
using Tagger.Rules.Configuration;
using Tagger.Rules.Custom;

namespace Tagger.Rules.Enrichers;

/// <summary>
/// Populates the rule context with file extension information derived from the tagging event.
/// </summary>
internal sealed class DocumentTypeContextEnricher : ITaggingRuleContextEnricher
{
    private static readonly IReadOnlyDictionary<string, string> MimeTypeExtensions =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["application/msword"] = ".doc",
            ["application/pdf"] = ".pdf",
            ["application/vnd.ms-excel"] = ".xls",
            ["application/vnd.ms-powerpoint"] = ".ppt",
            ["application/vnd.openxmlformats-officedocument.presentationml.presentation"] = ".pptx",
            ["application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"] = ".xlsx",
            ["application/vnd.openxmlformats-officedocument.wordprocessingml.document"] = ".docx",
            ["image/bmp"] = ".bmp",
            ["image/gif"] = ".gif",
            ["image/heic"] = ".heic",
            ["image/jpeg"] = ".jpg",
            ["image/png"] = ".png",
            ["image/svg+xml"] = ".svg",
            ["image/tiff"] = ".tiff",
            ["text/csv"] = ".csv",
            ["text/plain"] = ".txt",
        };

    private readonly EcmFileClient _client;
    private readonly ILogger<DocumentTypeContextEnricher> _logger;

    public DocumentTypeContextEnricher(EcmFileClient client, ILogger<DocumentTypeContextEnricher> logger)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Adds an <c>extension</c> field to the context when it can be resolved from metadata or the title.
    /// </summary>
    public void Enrich(TaggingRuleContextBuilder builder, ITaggingIntegrationEvent integrationEvent)
    {
        var extension = DocumentTypeRule.ResolveExtension(integrationEvent)
            ?? ResolveExtensionFromSdk(integrationEvent.DocumentId);
        if (!string.IsNullOrWhiteSpace(extension))
        {
            builder.AddField("extension", extension);
        }
    }

    private string? ResolveExtensionFromSdk(Guid documentId)
    {
        try
        {
            var document = _client.GetDocumentAsync(documentId, CancellationToken.None)
                .ConfigureAwait(false)
                .GetAwaiter()
                .GetResult();

            var latestVersion = document?.LatestVersion;
            if (latestVersion is null)
            {
                return null;
            }

            var extension = NormalizeExtension(Path.GetExtension(latestVersion.StorageKey));
            if (!string.IsNullOrWhiteSpace(extension))
            {
                return extension;
            }

            if (!string.IsNullOrWhiteSpace(latestVersion.MimeType))
            {
                var mimeType = latestVersion.MimeType.Split(';', 2)[0].Trim();
                if (MimeTypeExtensions.TryGetValue(mimeType, out extension))
                {
                    return extension;
                }
            }
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Failed to resolve extension for document {DocumentId} via ECM SDK.", documentId);
        }

        return null;
    }

    private static string? NormalizeExtension(string? extension)
    {
        if (string.IsNullOrWhiteSpace(extension))
        {
            return null;
        }

        var trimmed = extension.Trim();
        return trimmed.StartsWith(".", StringComparison.Ordinal) ? trimmed : $".{trimmed}";
    }
}
