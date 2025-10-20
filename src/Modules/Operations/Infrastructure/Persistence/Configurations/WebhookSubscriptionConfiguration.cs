using ECM.Operations.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECM.Operations.Infrastructure.Persistence.Configurations;

internal sealed class WebhookSubscriptionConfiguration : IEntityTypeConfiguration<WebhookSubscription>
{
    public void Configure(EntityTypeBuilder<WebhookSubscription> builder)
    {
        builder.ToTable("webhook", "ops");

        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd()
            .HasDefaultValueSql("uuid_generate_v4()");

        builder.Property(entity => entity.Name)
            .HasColumnName("name")
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(entity => entity.EventTypes)
            .HasColumnName("event_types")
            .HasColumnType("text[]")
            .IsRequired();

        builder.Property(entity => entity.Url)
            .HasColumnName("url")
            .HasMaxLength(512)
            .IsRequired();

        builder.Property(entity => entity.Secret)
            .HasColumnName("secret")
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(entity => entity.Description)
            .HasColumnName("description")
            .HasMaxLength(512)
            .IsRequired();

        builder.Property(entity => entity.IsActive)
            .HasColumnName("is_active")
            .ValueGeneratedOnAdd()
            .HasDefaultValue(true);

        builder.Property(entity => entity.CreatedAtUtc)
            .HasColumnName("created_at")
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.Property(entity => entity.DeactivatedAtUtc)
            .HasColumnName("deactivated_at");

        builder.HasIndex(entity => entity.IsActive)
            .HasDatabaseName("ops_webhook_active_idx");
    }
}
