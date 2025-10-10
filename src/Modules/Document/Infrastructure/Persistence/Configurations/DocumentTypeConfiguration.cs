using ECM.Modules.Document.Domain.DocumentTypes;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECM.Modules.Document.Infrastructure.Persistence.Configurations;

public sealed class DocumentTypeConfiguration : IEntityTypeConfiguration<DocumentType>
{
    public void Configure(EntityTypeBuilder<DocumentType> builder)
    {
        builder.ToTable("document_type");

        builder.HasKey(type => type.Id);

        builder.Property(type => type.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd()
            .HasDefaultValueSql("uuid_generate_v4()");

        builder.Property(type => type.TypeKey)
            .HasColumnName("type_key")
            .IsRequired();

        builder.HasIndex(type => type.TypeKey)
            .HasDatabaseName("IX_document_type_type_key")
            .IsUnique();

        builder.Property(type => type.TypeName)
            .HasColumnName("type_name")
            .IsRequired();

        builder.Property(type => type.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true);

        builder.Property(type => type.CreatedAtUtc)
            .HasColumnName("created_at")
            .HasColumnType("timestamptz")
            .HasDefaultValueSql("now()");

        builder.Navigation(type => type.Documents)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
