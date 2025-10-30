using ECM.Document.Domain.Documents;
using ECM.Document.Domain.Versions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECM.Document.Infrastructure.Persistence.Configurations;

public sealed class DocumentVersionConfiguration : IEntityTypeConfiguration<DocumentVersion>
{
    public void Configure(EntityTypeBuilder<DocumentVersion> builder)
    {
        builder.ToTable("version");

        builder.HasKey(version => version.Id);

        builder.Property(version => version.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        var documentIdProperty = builder.Property(version => version.DocumentId)
            .HasColumnName("document_id")
            .HasColumnType("uuid")
            .HasConversion(EfConverters.DocumentIdConverter)
            .IsRequired();

        documentIdProperty.Metadata.SetValueConverter(EfConverters.DocumentIdConverter);
        documentIdProperty.Metadata.SetValueComparer(EfConverters.DocumentIdComparer);

        builder.Property(version => version.VersionNo)
            .HasColumnName("version_no")
            .IsRequired();

        builder.Property(version => version.StorageKey)
            .HasColumnName("storage_key")
            .IsRequired();

        builder.Property(version => version.Bytes)
            .HasColumnName("bytes")
            .IsRequired();

        builder.Property(version => version.MimeType)
            .HasColumnName("mime_type")
            .IsRequired();

        builder.Property(version => version.Sha256)
            .HasColumnName("sha256")
            .IsRequired();

        builder.Property(version => version.CreatedBy)
            .HasColumnName("created_by")
            .IsRequired();

        builder.Property(version => version.CreatedAtUtc)
            .HasColumnName("created_at")
            .HasColumnType("timestamptz")
            .HasDefaultValueSql("now()");

        builder.HasIndex(version => version.CreatedBy)
            .HasDatabaseName("IX_version_created_by");

        builder.HasIndex(version => version.DocumentId)
            .HasDatabaseName("IX_version_document_id");

        builder.HasIndex(version => new { version.DocumentId, version.VersionNo })
            .HasDatabaseName("IX_version_document_id_version_no")
            .IsUnique();

        builder.HasMany(version => version.SignatureRequests)
            .WithOne(request => request.Version)
            .HasForeignKey(request => request.VersionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(version => version.SignatureRequests)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
