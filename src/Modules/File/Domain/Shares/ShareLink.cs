using System.Net;

namespace ECM.File.Domain.Shares;

public sealed class ShareLink
{
    public ShareLink(
        Guid id,
        string code,
        Guid ownerUserId,
        Guid documentId,
        Guid? versionId,
        ShareSubjectType subjectType,
        Guid? subjectId,
        SharePermission permissions,
        string? passwordHash,
        DateTimeOffset validFrom,
        DateTimeOffset? validTo,
        int? maxViews,
        int? maxDownloads,
        string fileName,
        string? fileExtension,
        string fileContentType,
        long fileSizeBytes,
        DateTimeOffset? fileCreatedAt,
        string? watermarkJson,
        IReadOnlyCollection<IPAddress>? allowedIps,
        DateTimeOffset createdAt,
        DateTimeOffset? revokedAt)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            throw new ArgumentException("Short code is required.", nameof(code));
        }

        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException("File name is required.", nameof(fileName));
        }

        if (string.IsNullOrWhiteSpace(fileContentType))
        {
            throw new ArgumentException("File content type is required.", nameof(fileContentType));
        }

        if (fileSizeBytes < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(fileSizeBytes));
        }

        Id = id;
        Code = code.Trim();
        OwnerUserId = ownerUserId;
        DocumentId = documentId;
        VersionId = versionId;
        SubjectType = subjectType;
        SubjectId = subjectType is ShareSubjectType.Public ? null : subjectId;
        Permissions = NormalizePermissions(permissions);
        PasswordHash = passwordHash;
        ValidFrom = validFrom;
        ValidTo = validTo;
        MaxViews = maxViews;
        MaxDownloads = maxDownloads;
        FileName = fileName.Trim();
        FileExtension = string.IsNullOrWhiteSpace(fileExtension) ? null : fileExtension.Trim();
        FileContentType = fileContentType.Trim();
        FileSizeBytes = fileSizeBytes;
        FileCreatedAt = fileCreatedAt;
        WatermarkJson = watermarkJson;
        AllowedIps = allowedIps ?? Array.Empty<IPAddress>();
        CreatedAt = createdAt;
        RevokedAt = revokedAt;
    }

    public Guid Id { get; }

    public string Code { get; }

    public Guid OwnerUserId { get; }

    public Guid DocumentId { get; }

    public Guid? VersionId { get; }

    public ShareSubjectType SubjectType { get; }

    public Guid? SubjectId { get; }

    public SharePermission Permissions { get; private set; }

    public string? PasswordHash { get; private set; }

    public DateTimeOffset ValidFrom { get; private set; }

    public DateTimeOffset? ValidTo { get; private set; }

    public int? MaxViews { get; private set; }

    public int? MaxDownloads { get; private set; }

    public string FileName { get; private set; }

    public string? FileExtension { get; private set; }

    public string FileContentType { get; private set; }

    public long FileSizeBytes { get; private set; }

    public DateTimeOffset? FileCreatedAt { get; private set; }

    public string? WatermarkJson { get; private set; }

    public IReadOnlyCollection<IPAddress> AllowedIps { get; private set; }

    public DateTimeOffset CreatedAt { get; }

    public DateTimeOffset? RevokedAt { get; private set; }

    public ShareLinkStatus GetStatus(DateTimeOffset now)
    {
        if (RevokedAt.HasValue)
        {
            return ShareLinkStatus.Revoked;
        }

        if (now < ValidFrom)
        {
            return ShareLinkStatus.Draft;
        }

        if (ValidTo.HasValue && now > ValidTo.Value)
        {
            return ShareLinkStatus.Expired;
        }

        return ShareLinkStatus.Active;
    }

    public bool HasPermission(SharePermission permission) => (Permissions & permission) == permission;

    public bool RequiresPassword => !string.IsNullOrWhiteSpace(PasswordHash);

    public bool IsExpired(DateTimeOffset now) => ValidTo.HasValue && now > ValidTo.Value;

    public bool IsActive(DateTimeOffset now) => GetStatus(now) == ShareLinkStatus.Active;

    public void Revoke(DateTimeOffset revokedAt)
    {
        RevokedAt = revokedAt;
    }

    public void UpdateWindow(DateTimeOffset validFrom, DateTimeOffset? validTo)
    {
        ValidFrom = validFrom;
        ValidTo = validTo;
    }

    public void UpdateQuotas(int? maxViews, int? maxDownloads)
    {
        MaxViews = maxViews;
        MaxDownloads = maxDownloads;
    }

    public void UpdatePermissions(SharePermission permissions)
    {
        Permissions = NormalizePermissions(permissions);
    }

    public void UpdatePasswordHash(string? passwordHash)
    {
        PasswordHash = string.IsNullOrWhiteSpace(passwordHash) ? null : passwordHash;
    }

    public void UpdateFileMetadata(
        string fileName,
        string? fileExtension,
        string fileContentType,
        long fileSizeBytes,
        DateTimeOffset? fileCreatedAt)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException("File name is required.", nameof(fileName));
        }

        if (string.IsNullOrWhiteSpace(fileContentType))
        {
            throw new ArgumentException("File content type is required.", nameof(fileContentType));
        }

        if (fileSizeBytes < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(fileSizeBytes));
        }

        FileName = fileName.Trim();
        FileExtension = string.IsNullOrWhiteSpace(fileExtension) ? null : fileExtension.Trim();
        FileContentType = fileContentType.Trim();
        FileSizeBytes = fileSizeBytes;
        FileCreatedAt = fileCreatedAt;
    }

    public void UpdateWatermark(string? watermarkJson)
    {
        WatermarkJson = watermarkJson;
    }

    public void UpdateAllowedIps(IEnumerable<IPAddress>? allowedIps)
    {
        AllowedIps = allowedIps?.ToArray() ?? Array.Empty<IPAddress>();
    }

    private static SharePermission NormalizePermissions(SharePermission permissions)
    {
        if (permissions == SharePermission.None)
        {
            return SharePermission.View;
        }

        return permissions;
    }
}
