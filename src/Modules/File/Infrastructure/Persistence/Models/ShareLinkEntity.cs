namespace ECM.File.Infrastructure.Persistence.Models;

public sealed class ShareLinkEntity
{
    public Guid Id { get; set; }

    public string Code { get; set; } = string.Empty;

    public Guid OwnerUserId { get; set; }

    public Guid DocumentId { get; set; }

    public Guid? VersionId { get; set; }

    public string SubjectType { get; set; } = string.Empty;

    public Guid? SubjectId { get; set; }

    public string[] Permissions { get; set; } = [];

    public string? PasswordHash { get; set; }

    public DateTimeOffset ValidFrom { get; set; }

    public DateTimeOffset? ValidTo { get; set; }

    public int? MaxViews { get; set; }

    public int? MaxDownloads { get; set; }

    public string FileName { get; set; } = string.Empty;

    public string? FileExtension { get; set; }

    public string FileContentType { get; set; } = string.Empty;

    public long FileSizeBytes { get; set; }

    public DateTimeOffset? FileCreatedAt { get; set; }

    public string? WatermarkJson { get; set; }

    public string[] AllowedIps { get; set; } = [];

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? RevokedAt { get; set; }
}
