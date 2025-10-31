using System;
using System.Collections.Generic;
using System.IO;

namespace AppGateway.Api.Controllers.Documents;

internal static class DocumentRequestNormalization
{
    public static Guid? NormalizeGuid(Guid? value)
    {
        return value is null || value == Guid.Empty ? null : value;
    }

    public static string NormalizeString(string? value, string fallback)
    {
        return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }

    public static string? NormalizeOptional(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    public static string NormalizeTitle(string? title, string? fileName)
    {
        if (!string.IsNullOrWhiteSpace(title))
        {
            return title.Trim();
        }

        if (!string.IsNullOrWhiteSpace(fileName))
        {
            var nameWithoutExtension = Path.GetFileNameWithoutExtension(fileName);
            if (!string.IsNullOrWhiteSpace(nameWithoutExtension))
            {
                return nameWithoutExtension.Trim();
            }

            return fileName.Trim();
        }

        return "Untitled document";
    }

    public static IReadOnlyList<Guid> NormalizeGroupSelection(
        Guid? groupId,
        IReadOnlyCollection<Guid> groupIds,
        out Guid? primaryGroupId)
    {
        var buffer = new List<Guid>();
        var seen = new HashSet<Guid>();

        if (groupId.HasValue && groupId.Value != Guid.Empty && seen.Add(groupId.Value))
        {
            buffer.Add(groupId.Value);
        }

        if (groupIds is not null)
        {
            foreach (var id in groupIds)
            {
                if (id == Guid.Empty)
                {
                    continue;
                }

                if (seen.Add(id))
                {
                    buffer.Add(id);
                }
            }
        }

        primaryGroupId = buffer.Count > 0 ? buffer[0] : null;
        return buffer;
    }
}
