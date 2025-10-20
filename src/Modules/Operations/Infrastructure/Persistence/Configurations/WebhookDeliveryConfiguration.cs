using ECM.Operations.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECM.Operations.Infrastructure.Persistence.Configurations;

internal sealed class WebhookDeliveryConfiguration : IEntityTypeConfiguration<WebhookDelivery>
{
    public void Configure(EntityTypeBuilder<WebhookDelivery> builder)
    {
        builder.ToTable("webhook_delivery", "ops");

        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd()
            .HasDefaultValueSql("uuid_generate_v4()");

        builder.Property(entity => entity.WebhookId)
            .HasColumnName("webhook_id")
            .IsRequired();

        builder.Property(entity => entity.EventType)
            .HasColumnName("event_type")
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(entity => entity.Payload)
            .HasColumnName("payload")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(entity => entity.Status)
            .HasColumnName("status")
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(entity => entity.AttemptCount)
            .HasColumnName("attempt_count")
            .ValueGeneratedOnAdd()
            .HasDefaultValue(0);

        builder.Property(entity => entity.EnqueuedAtUtc)
            .HasColumnName("enqueued_at")
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.Property(entity => entity.DeliveredAtUtc)
            .HasColumnName("delivered_at");

        builder.Property(entity => entity.Error)
            .HasColumnName("error")
            .HasMaxLength(1024);

        builder.HasOne(entity => entity.Webhook)
            .WithMany()
            .HasForeignKey(entity => entity.WebhookId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(entity => new { entity.WebhookId, entity.Status })
            .HasDatabaseName("ops_webhook_delivery_status_idx");
    }
}
