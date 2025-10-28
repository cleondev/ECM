using ECM.Document.Infrastructure.Persistence.ReadModels;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECM.Document.Infrastructure.Persistence.Configurations;

public sealed class EffectiveAclFlatEntryConfiguration : IEntityTypeConfiguration<EffectiveAclFlatEntry>
{
    public void Configure(EntityTypeBuilder<EffectiveAclFlatEntry> builder)
    {
        builder.ToTable("effective_acl_flat");

        builder.HasKey(entry => new { entry.DocumentId, entry.UserId, entry.IdempotencyKey });

        builder.Property(entry => entry.DocumentId)
            .HasColumnName("document_id");

        builder.Property(entry => entry.UserId)
            .HasColumnName("user_id");

        builder.Property(entry => entry.ValidToUtc)
            .HasColumnName("valid_to")
            .HasColumnType("timestamptz");

        builder.Property(entry => entry.Source)
            .HasColumnName("source")
            .IsRequired();

        builder.Property(entry => entry.IdempotencyKey)
            .HasColumnName("idempotency_key")
            .IsRequired();

        builder.Property(entry => entry.UpdatedAtUtc)
            .HasColumnName("updated_at")
            .HasColumnType("timestamptz")
            .HasDefaultValueSql("now()");

        builder.HasIndex(entry => new { entry.UserId, entry.ValidToUtc, entry.DocumentId })
            .HasDatabaseName("doc_effective_acl_flat_user_document_idx");
    }
}
