using ECM.Operations.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECM.Operations.Infrastructure.Persistence.Configurations;

internal sealed class AuditEventConfiguration : IEntityTypeConfiguration<AuditEvent>
{
    public void Configure(EntityTypeBuilder<AuditEvent> builder)
    {
        builder.ToTable("audit_event", "ops");

        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd();

        builder.Property(entity => entity.OccurredAtUtc)
            .HasColumnName("occurred_at")
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.Property(entity => entity.ActorId)
            .HasColumnName("actor_id");

        builder.Property(entity => entity.Action)
            .HasColumnName("action")
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(entity => entity.ObjectType)
            .HasColumnName("object_type")
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(entity => entity.ObjectId)
            .HasColumnName("object_id")
            .IsRequired();

        builder.Property(entity => entity.Details)
            .HasColumnName("details")
            .HasColumnType("jsonb")
            .HasDefaultValueSql("'{}'::jsonb")
            .IsRequired();

        builder.HasIndex(entity => new { entity.ObjectType, entity.ObjectId })
            .HasDatabaseName("ops_audit_obj_idx");

        builder.HasIndex(entity => entity.OccurredAtUtc)
            .HasDatabaseName("ops_audit_time_idx");
    }
}
