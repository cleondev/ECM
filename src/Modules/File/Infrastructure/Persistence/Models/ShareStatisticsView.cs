namespace ECM.File.Infrastructure.Persistence.Models;

public sealed class ShareStatisticsView
{
    public Guid ShareId { get; set; }

    public long Views { get; set; }

    public long Downloads { get; set; }

    public long Failures { get; set; }

    public DateTimeOffset? LastAccess { get; set; }
}
