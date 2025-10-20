using ECM.Operations.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECM.Operations.Infrastructure.Persistence.Configurations;

internal sealed class RetentionCandidateConfiguration : IEntityTypeConfiguration<RetentionCandidate>
{
    public void Configure(EntityTypeBuilder<RetentionCandidate> builder)
    {
        builder.ToTable("retention_candidate", "ops");

        builder.HasKey(entity => entity.DocumentId);

        builder.Property(entity => entity.DocumentId)
            .HasColumnName("document_id");

        builder.Property(entity => entity.PolicyId)
            .HasColumnName("policy_id")
            .IsRequired();

        builder.Property(entity => entity.DueAtUtc)
            .HasColumnName("due_at")
            .IsRequired();

        builder.Property(entity => entity.Reason)
            .HasColumnName("reason")
            .HasMaxLength(512);

        builder.HasOne(entity => entity.Policy)
            .WithMany()
            .HasForeignKey(entity => entity.PolicyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(entity => entity.DueAtUtc)
            .HasDatabaseName("ops_retention_due_idx");
    }
}
