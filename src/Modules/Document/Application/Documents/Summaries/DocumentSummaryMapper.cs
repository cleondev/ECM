using System;
using ECM.Document.Domain.Documents;
using ECM.Document.Domain.Versions;

namespace ECM.Document.Application.Documents.Summaries;

internal static class DocumentSummaryMapper
{
    public static DocumentSummary ToSummary(Document document)
    {
        ArgumentNullException.ThrowIfNull(document);

        return new DocumentSummary(
            document.Id.Value,
            document.Title.Value,
            document.DocType,
            document.Status,
            document.Sensitivity,
            document.OwnerId,
            document.CreatedBy,
            document.Department,
            document.CreatedAtUtc,
            document.UpdatedAtUtc,
            document.TypeId);
    }

    public static DocumentWithVersionSummary ToSummary(Document document, DocumentVersion latestVersion)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(latestVersion);

        return new DocumentWithVersionSummary(
            document.Id.Value,
            document.Title.Value,
            document.DocType,
            document.Status,
            document.Sensitivity,
            document.OwnerId,
            document.CreatedBy,
            document.Department,
            document.CreatedAtUtc,
            document.UpdatedAtUtc,
            document.TypeId,
            ToSummary(latestVersion));
    }

    public static DocumentVersionSummary ToSummary(DocumentVersion version)
    {
        ArgumentNullException.ThrowIfNull(version);

        return new DocumentVersionSummary(
            version.Id,
            version.VersionNo,
            version.StorageKey,
            version.Bytes,
            version.MimeType,
            version.Sha256,
            version.CreatedBy,
            version.CreatedAtUtc);
    }
}
