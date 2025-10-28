using ECM.Ocr.Domain.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECM.Ocr.Infrastructure.Persistence.Configurations;

internal sealed class OcrPageTextConfiguration : IEntityTypeConfiguration<OcrPageText>
{
    public void Configure(EntityTypeBuilder<OcrPageText> builder)
    {
        builder.ToTable("page_text", "ocr");

        builder.HasKey(page => new { page.DocumentId, page.VersionId, page.PageNo });

        builder.Property(page => page.PageNo).HasColumnName("page_no");
        builder.Property(page => page.Content).HasColumnName("content").IsRequired();
    }
}
