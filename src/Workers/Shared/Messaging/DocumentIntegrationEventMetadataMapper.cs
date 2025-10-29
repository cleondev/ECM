using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Workers.Shared.Messaging;

public static class DocumentIntegrationEventMetadataMapper
{
    public static IDictionary<string, string>? ToMetadataDictionary(DocumentIntegrationEventPayload payload)
    {
        ArgumentNullException.ThrowIfNull(payload);

        Dictionary<string, string>? metadata = null;

        if (payload.Metadata is JsonElement element && element.ValueKind == JsonValueKind.Object)
        {
            metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var property in element.EnumerateObject())
            {
                var key = property.Name?.Trim();
                if (string.IsNullOrEmpty(key))
                {
                    continue;
                }

                var values = ExtractValues(property.Value);
                if (values.Count == 0)
                {
                    continue;
                }

                metadata[key] = string.Join(',', values);
            }
        }

        if (payload.GroupIds is { Count: > 0 })
        {
            var groups = payload.GroupIds
                .Where(id => id != Guid.Empty)
                .Select(id => id.ToString())
                .ToArray();

            if (groups.Length > 0)
            {
                metadata ??= new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                metadata["groupIds"] = string.Join(',', groups);
            }
        }

        if (metadata is null)
        {
            return null;
        }

        metadata.Remove("department");

        return metadata.Count == 0 ? null : metadata;
    }

    private static IReadOnlyList<string> ExtractValues(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.String:
                var text = element.GetString();
                return string.IsNullOrWhiteSpace(text)
                    ? Array.Empty<string>()
                    : new[] { text.Trim() };

            case JsonValueKind.Number:
            case JsonValueKind.True:
            case JsonValueKind.False:
                var scalar = element.ToString();
                return string.IsNullOrWhiteSpace(scalar)
                    ? Array.Empty<string>()
                    : new[] { scalar.Trim() };

            case JsonValueKind.Array:
                var buffer = new List<string>();
                foreach (var item in element.EnumerateArray())
                {
                    foreach (var extracted in ExtractValues(item))
                    {
                        if (!string.IsNullOrWhiteSpace(extracted))
                        {
                            buffer.Add(extracted);
                        }
                    }
                }

                return buffer;

            default:
                return Array.Empty<string>();
        }
    }
}
