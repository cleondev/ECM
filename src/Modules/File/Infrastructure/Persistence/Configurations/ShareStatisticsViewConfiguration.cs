using ECM.File.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECM.File.Infrastructure.Persistence.Configurations;

public sealed class ShareStatisticsViewConfiguration : IEntityTypeConfiguration<ShareStatisticsView>
{
    public void Configure(EntityTypeBuilder<ShareStatisticsView> builder)
    {
        builder.ToView("share_stats");
        builder.HasNoKey();

        builder.Property(view => view.ShareId)
            .HasColumnName("share_id");

        builder.Property(view => view.Views)
            .HasColumnName("views");

        builder.Property(view => view.Downloads)
            .HasColumnName("downloads");

        builder.Property(view => view.Failures)
            .HasColumnName("failures");

        builder.Property(view => view.LastAccess)
            .HasColumnName("last_access")
            .HasColumnType("timestamptz");
    }
}
