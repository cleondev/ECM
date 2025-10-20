using ECM.Operations.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECM.Operations.Infrastructure.Persistence.Configurations;

internal sealed class UserNotificationConfiguration : IEntityTypeConfiguration<UserNotification>
{
    public void Configure(EntityTypeBuilder<UserNotification> builder)
    {
        builder.ToTable("notification", "ops");

        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd()
            .HasDefaultValueSql("uuid_generate_v4()");

        builder.Property(entity => entity.UserId)
            .HasColumnName("user_id")
            .IsRequired();

        builder.Property(entity => entity.Type)
            .HasColumnName("type")
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(entity => entity.Title)
            .HasColumnName("title")
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(entity => entity.Message)
            .HasColumnName("message")
            .HasMaxLength(1024)
            .IsRequired();

        builder.Property(entity => entity.Payload)
            .HasColumnName("payload")
            .HasColumnType("jsonb")
            .HasDefaultValueSql("'{}'::jsonb")
            .IsRequired();

        builder.Property(entity => entity.IsRead)
            .HasColumnName("is_read")
            .HasDefaultValue(false);

        builder.Property(entity => entity.CreatedAtUtc)
            .HasColumnName("created_at")
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.Property(entity => entity.ReadAtUtc)
            .HasColumnName("read_at");

        builder.HasIndex(entity => new { entity.UserId, entity.IsRead, entity.CreatedAtUtc })
            .HasDatabaseName("ops_notification_user_idx");
    }
}
