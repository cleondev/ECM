namespace ECM.File.Infrastructure.Persistence.Models;

public sealed class ShareAccessEventEntity
{
    public long Id { get; set; }

    public Guid ShareId { get; set; }

    public ShareLinkEntity? Share { get; set; }

    public DateTimeOffset OccurredAt { get; set; }

    public string Action { get; set; } = string.Empty;

    public string? RemoteIp { get; set; }

    public string? UserAgent { get; set; }

    public bool Ok { get; set; }
}
