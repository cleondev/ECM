using ECM.Ocr.Domain.Annotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECM.Ocr.Infrastructure.Persistence.Configurations;

internal sealed class OcrAnnotationConfiguration : IEntityTypeConfiguration<OcrAnnotation>
{
    public void Configure(EntityTypeBuilder<OcrAnnotation> builder)
    {
        builder.ToTable("annotation", "ocr");

        builder.HasKey(annotation => annotation.Id);

        builder.Property(annotation => annotation.DocumentId).HasColumnName("document_id");
        builder.Property(annotation => annotation.VersionId).HasColumnName("version_id");
        builder.Property(annotation => annotation.TemplateId).HasColumnName("template_id");
        builder.Property(annotation => annotation.FieldKey).HasColumnName("field_key");
        builder.Property(annotation => annotation.ValueText).HasColumnName("value_text");
        builder.Property(annotation => annotation.BoundingBox).HasColumnName("bbox_abs").HasColumnType("jsonb");
        builder.Property(annotation => annotation.Confidence).HasColumnName("confidence").HasColumnType("numeric");
        builder.Property(annotation => annotation.Source).HasColumnName("source");
        builder.Property(annotation => annotation.CreatedBy).HasColumnName("created_by");
        builder.Property(annotation => annotation.CreatedAt).HasColumnName("created_at");

        builder.HasOne(annotation => annotation.Template)
            .WithMany(template => template.Annotations)
            .HasForeignKey(annotation => annotation.TemplateId)
            .OnDelete(DeleteBehavior.NoAction);
    }
}
