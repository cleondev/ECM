using ECM.Webhook.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECM.Webhook.Infrastructure.Persistence.Configurations;

internal sealed class WebhookDeliveryConfiguration : IEntityTypeConfiguration<WebhookDelivery>
{
    public void Configure(EntityTypeBuilder<WebhookDelivery> builder)
    {
        builder.ToTable("webhook_delivery", "webhook");

        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(entity => entity.RequestId)
            .HasColumnName("request_id")
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(entity => entity.EndpointKey)
            .HasColumnName("endpoint_key")
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(entity => entity.AttemptCount)
            .HasColumnName("attempt_count")
            .ValueGeneratedOnAdd()
            .HasDefaultValue(0);

        builder.Property(entity => entity.Status)
            .HasColumnName("status")
            .HasMaxLength(32)
            .IsRequired()
            .HasDefaultValue("Pending");

        builder.Property(entity => entity.LastAttemptAt)
            .HasColumnName("last_attempt_at")
            .IsRequired();

        builder.HasIndex(entity => new { entity.RequestId, entity.EndpointKey })
            .IsUnique()
            .HasDatabaseName("ux_webhook_delivery_request_endpoint");

        builder.HasIndex(entity => entity.Status)
            .HasDatabaseName("ix_webhook_delivery_status");
    }
}
