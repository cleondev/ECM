using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace ECM.Document.Api.Documents.Requests;

public sealed class CreateDocumentRequest
{
    public string? Title { get; init; }

    public string? DocType { get; init; }

    public string? Status { get; init; }

    public Guid? OwnerId { get; init; }

    public Guid? CreatedBy { get; init; }

    public Guid? GroupId { get; init; }

    public Guid[]? GroupIds { get; init; }

    public string? Sensitivity { get; init; }

    public Guid? DocumentTypeId { get; init; }

    [Required]
    public IFormFile? File { get; init; }

    public static async ValueTask<CreateDocumentRequest?> BindAsync(HttpContext context)
    {
        if (!context.Request.HasFormContentType)
        {
            return null;
        }

        var form = await context.Request.ReadFormAsync(context.RequestAborted);

        var request = new CreateDocumentRequest
        {
            Title = GetString(form, nameof(Title)),
            DocType = GetString(form, nameof(DocType)),
            Status = GetString(form, nameof(Status)),
            OwnerId = GetGuid(form, nameof(OwnerId)),
            CreatedBy = GetGuid(form, nameof(CreatedBy)),
            GroupId = GetGuid(form, nameof(GroupId)) ?? GetGuid(form, "PrimaryGroupId"),
            GroupIds = GetGuidList(form, nameof(GroupIds)),
            Sensitivity = GetString(form, nameof(Sensitivity)),
            DocumentTypeId = GetGuid(form, nameof(DocumentTypeId)),
            File = GetFile(form, nameof(File))
        };

        return request;
    }

    private static string? GetString(IFormCollection form, string propertyName)
    {
        if (form.TryGetValue(propertyName, out var value) && !StringValues.IsNullOrEmpty(value))
        {
            return value.ToString();
        }

        var camelCase = char.ToLowerInvariant(propertyName[0]) + propertyName[1..];
        if (form.TryGetValue(camelCase, out value) && !StringValues.IsNullOrEmpty(value))
        {
            return value.ToString();
        }

        return null;
    }

    private static Guid? GetGuid(IFormCollection form, string propertyName)
    {
        var value = GetString(form, propertyName);
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return Guid.TryParse(value, out var parsed) ? parsed : null;
    }

    private static Guid[]? GetGuidList(IFormCollection form, string propertyName)
    {
        var candidates = new[]
        {
            propertyName,
            $"{propertyName}[]",
            "group_ids",
            "group_ids[]",
            "groupIds",
            "groupIds[]"
        };

        foreach (var candidate in candidates)
        {
            if (!form.TryGetValue(candidate, out var values) || values.Count == 0)
            {
                continue;
            }

            var parsed = ParseGuidValues(values);
            if (parsed.Length > 0)
            {
                return parsed;
            }
        }

        return null;
    }

    private static Guid[] ParseGuidValues(StringValues values)
    {
        var buffer = new List<Guid>();
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

        if (values.Count > 1)
        {
            foreach (var value in values)
            {
                if (Guid.TryParse(value, out var guid))
                {
                    Add(guid);
                }
            }

            return buffer.ToArray();
        }

        var raw = values[0];
        if (string.IsNullOrWhiteSpace(raw))
        {
            return Array.Empty<Guid>();
        }

        if (raw.TrimStart().StartsWith("[", StringComparison.Ordinal))
        {
            try
            {
                var parsed = JsonSerializer.Deserialize<string[]>(raw);
                if (parsed is not null)
                {
                    foreach (var candidate in parsed)
                    {
                        if (Guid.TryParse(candidate, out var guid))
                        {
                            Add(guid);
                        }
                    }
                }
            }
            catch (JsonException)
            {
                // Ignore malformed JSON and fall back to delimiter-based parsing.
            }
        }

        if (buffer.Count > 0)
        {
            return buffer.ToArray();
        }

        foreach (var segment in raw.Split(
                     new[] { ',', ';' },
                     StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (Guid.TryParse(segment, out var guid))
            {
                Add(guid);
            }
        }

        return buffer.ToArray();
    }

    private static IFormFile? GetFile(IFormCollection form, string propertyName)
    {
        return form.Files.GetFile(propertyName)
            ?? form.Files.GetFile(char.ToLowerInvariant(propertyName[0]) + propertyName[1..])
            ?? form.Files.FirstOrDefault();
    }
}
