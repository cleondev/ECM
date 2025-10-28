using ECM.Ocr.Domain.Templates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECM.Ocr.Infrastructure.Persistence.Configurations;

internal sealed class OcrFieldDefinitionConfiguration : IEntityTypeConfiguration<OcrFieldDefinition>
{
    public void Configure(EntityTypeBuilder<OcrFieldDefinition> builder)
    {
        builder.ToTable("field_def", "ocr");

        builder.HasKey(field => field.Id);

        builder.Property(field => field.TemplateId).HasColumnName("template_id");
        builder.Property(field => field.FieldKey).HasColumnName("field_key").IsRequired();
        builder.Property(field => field.BoundingBoxRelative).HasColumnName("bbox_rel").HasColumnType("jsonb");
        builder.Property(field => field.Anchor).HasColumnName("anchor").HasColumnType("jsonb");
        builder.Property(field => field.Validator).HasColumnName("validator");
        builder.Property(field => field.Required).HasColumnName("required");
        builder.Property(field => field.OrderNo).HasColumnName("order_no");
    }
}
