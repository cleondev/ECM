using System;
using System.Collections.Generic;
using ECM.Document.Domain.Documents;
using ECM.Document.Domain.Versions;
using DomainDocument = ECM.Document.Domain.Documents.Document;

namespace ECM.Document.Application.Documents.Summaries;

internal static class Mapping
{
    public static DocumentSummaryResult ToResult(this DomainDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        return new DocumentSummaryResult(
            document.Id.Value,
            document.Title.Value,
            document.DocType,
            document.Status,
            document.Sensitivity,
            document.OwnerId,
            document.CreatedBy,
            document.GroupId,
            ToGroupIds(document.GroupId),
            document.CreatedAtUtc,
            document.UpdatedAtUtc,
            document.TypeId);
    }

    public static DocumentWithVersionResult ToResult(this DomainDocument document, DocumentVersion latestVersion)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(latestVersion);

        return new DocumentWithVersionResult(
            document.Id.Value,
            document.Title.Value,
            document.DocType,
            document.Status,
            document.Sensitivity,
            document.OwnerId,
            document.CreatedBy,
            document.GroupId,
            ToGroupIds(document.GroupId),
            document.CreatedAtUtc,
            document.UpdatedAtUtc,
            document.TypeId,
            latestVersion.ToResult());
    }

    public static DocumentVersionResult ToResult(this DocumentVersion version)
    {
        ArgumentNullException.ThrowIfNull(version);

        return new DocumentVersionResult(
            version.Id,
            version.VersionNo,
            version.StorageKey,
            version.Bytes,
            version.MimeType,
            version.Sha256,
            version.CreatedBy,
            version.CreatedAtUtc);
    }

    private static IReadOnlyCollection<Guid> ToGroupIds(Guid? groupId)
    {
        if (groupId.HasValue && groupId.Value != Guid.Empty)
        {
            return [groupId.Value];
        }

        return [];
    }
}
