using ECM.Ocr.Domain.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECM.Ocr.Infrastructure.Persistence.Configurations;

internal sealed class OcrDocumentResultConfiguration : IEntityTypeConfiguration<OcrDocumentResult>
{
    public void Configure(EntityTypeBuilder<OcrDocumentResult> builder)
    {
        builder.ToTable("result", "ocr");

        builder.HasKey(result => new { result.DocumentId, result.VersionId });

        builder.Property(result => result.Pages).IsRequired();
        builder.Property(result => result.Lang).HasColumnName("lang");
        builder.Property(result => result.Summary).HasColumnName("summary");
        builder.Property(result => result.CreatedAt).HasColumnName("created_at");

        builder.HasMany(result => result.PageTexts)
            .WithOne(page => page.Result)
            .HasForeignKey(page => new { page.DocumentId, page.VersionId })
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(result => result.Extractions)
            .WithOne(extraction => extraction.Result)
            .HasForeignKey(extraction => new { extraction.DocumentId, extraction.VersionId })
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(result => result.Annotations)
            .WithOne(annotation => annotation.Result)
            .HasForeignKey(annotation => new { annotation.DocumentId, annotation.VersionId })
            .OnDelete(DeleteBehavior.Cascade);
    }
}
