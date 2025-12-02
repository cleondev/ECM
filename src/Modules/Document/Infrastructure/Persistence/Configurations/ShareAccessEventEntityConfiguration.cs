using ECM.Document.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECM.Document.Infrastructure.Persistence.Configurations;

public sealed class ShareAccessEventEntityConfiguration : IEntityTypeConfiguration<ShareAccessEventEntity>
{
    public void Configure(EntityTypeBuilder<ShareAccessEventEntity> builder)
    {
        builder.ToTable("share_access_event", "file");

        builder.HasKey(evt => evt.Id);

        builder.Property(evt => evt.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(evt => evt.ShareId)
            .HasColumnName("share_id")
            .IsRequired();

        builder.HasOne(evt => evt.Share)
            .WithMany()
            .HasForeignKey(evt => evt.ShareId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(evt => evt.OccurredAt)
            .HasColumnName("occurred_at")
            .HasColumnType("timestamptz")
            .HasDefaultValueSql("now()");

        builder.Property(evt => evt.Action)
            .HasColumnName("action")
            .IsRequired();

        builder.Property(evt => evt.RemoteIp)
            .HasColumnName("remote_ip");

        builder.Property(evt => evt.UserAgent)
            .HasColumnName("user_agent");

        builder.Property(evt => evt.Ok)
            .HasColumnName("ok")
            .IsRequired();
    }
}
