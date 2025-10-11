using ECM.AccessControl.Infrastructure.Outbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECM.AccessControl.Infrastructure.Persistence.Configurations;

internal sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("outbox", "ops");

        builder.HasKey(message => message.Id);

        builder.Property(message => message.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(message => message.Aggregate)
            .HasColumnName("aggregate")
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(message => message.AggregateId)
            .HasColumnName("aggregate_id")
            .IsRequired();

        builder.Property(message => message.Type)
            .HasColumnName("type")
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(message => message.Payload)
            .HasColumnName("payload")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(message => message.OccurredAtUtc)
            .HasColumnName("occurred_at")
            .IsRequired();

        builder.Property(message => message.ProcessedAtUtc)
            .HasColumnName("processed_at");
    }
}
