using ECM.Ocr.Domain.Templates;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECM.Ocr.Infrastructure.Persistence.Configurations;

internal sealed class OcrTemplateConfiguration : IEntityTypeConfiguration<OcrTemplate>
{
    public void Configure(EntityTypeBuilder<OcrTemplate> builder)
    {
        builder.ToTable("template", "ocr");

        builder.HasKey(template => template.Id);

        builder.Property(template => template.Name).HasColumnName("name").IsRequired();
        builder.Property(template => template.Version).HasColumnName("version");
        builder.Property(template => template.PageSide).HasColumnName("page_side");
        builder.Property(template => template.SizeRatio).HasColumnName("size_ratio");
        builder.Property(template => template.IsActive).HasColumnName("is_active");

        builder.HasMany(template => template.Fields)
            .WithOne(field => field.Template)
            .HasForeignKey(field => field.TemplateId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
