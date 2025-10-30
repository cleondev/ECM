using ECM.Document.Domain.Documents;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECM.Document.Infrastructure.Persistence.Configurations;

public sealed class DocumentMetadataConfiguration : IEntityTypeConfiguration<DocumentMetadata>
{
    public void Configure(EntityTypeBuilder<DocumentMetadata> builder)
    {
        builder.ToTable("metadata");

        builder.HasKey(metadata => metadata.DocumentId);

        var documentIdProperty = builder.Property(metadata => metadata.DocumentId)
            .HasColumnName("document_id")
            .HasColumnType("uuid")
            .HasConversion(EfConverters.DocumentIdConverter);

        documentIdProperty.Metadata.SetValueConverter(EfConverters.DocumentIdConverter);
        documentIdProperty.Metadata.SetValueComparer(EfConverters.DocumentIdComparer);

        builder.Property(metadata => metadata.Data)
            .HasColumnName("data")
            .ConfigureJsonDocument();
    }
}
