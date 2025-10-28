using ECM.Ocr.Domain.Extractions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECM.Ocr.Infrastructure.Persistence.Configurations;

internal sealed class OcrExtractionConfiguration : IEntityTypeConfiguration<OcrExtraction>
{
    public void Configure(EntityTypeBuilder<OcrExtraction> builder)
    {
        builder.ToTable("extraction", "ocr");

        builder.HasKey(extraction => new { extraction.DocumentId, extraction.VersionId, extraction.FieldKey });

        builder.Property(extraction => extraction.DocumentId).HasColumnName("document_id");
        builder.Property(extraction => extraction.VersionId).HasColumnName("version_id");
        builder.Property(extraction => extraction.FieldKey).HasColumnName("field_key");
        builder.Property(extraction => extraction.ValueText).HasColumnName("value_text");
        builder.Property(extraction => extraction.Confidence).HasColumnName("confidence").HasColumnType("numeric");
        builder.Property(extraction => extraction.Provenance).HasColumnName("provenance").HasColumnType("jsonb");
    }
}
