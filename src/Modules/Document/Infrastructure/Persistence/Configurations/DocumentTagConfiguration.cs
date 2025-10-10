using ECM.Document.Domain.Documents;
using ECM.Document.Domain.Tags;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECM.Document.Infrastructure.Persistence.Configurations;

public sealed class DocumentTagConfiguration : IEntityTypeConfiguration<DocumentTag>
{
    public void Configure(EntityTypeBuilder<DocumentTag> builder)
    {
        builder.ToTable("document_tag");

        builder.HasKey(tag => new { tag.DocumentId, tag.TagId });

        builder.Property(tag => tag.DocumentId)
            .HasColumnName("document_id")
            .HasConversion(id => id.Value, value => DocumentId.FromGuid(value));

        builder.Property(tag => tag.TagId)
            .HasColumnName("tag_id");

        builder.Property(tag => tag.AppliedBy)
            .HasColumnName("applied_by");

        builder.Property(tag => tag.AppliedAtUtc)
            .HasColumnName("applied_at")
            .HasColumnType("timestamptz")
            .HasDefaultValueSql("now()");

        builder.HasIndex(tag => tag.AppliedBy)
            .HasDatabaseName("IX_document_tag_applied_by");

        builder.HasIndex(tag => tag.TagId)
            .HasDatabaseName("IX_document_tag_tag_id");

        builder.HasOne(tag => tag.Document)
            .WithMany(document => document.Tags)
            .HasForeignKey(tag => tag.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(tag => tag.Tag)
            .WithMany(label => label.DocumentTags)
            .HasForeignKey(tag => tag.TagId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
