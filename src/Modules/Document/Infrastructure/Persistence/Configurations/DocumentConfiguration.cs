using ECM.Document.Domain.Documents;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using DomainDocument = ECM.Document.Domain.Documents.Document;

namespace ECM.Document.Infrastructure.Persistence.Configurations;

public sealed class DocumentConfiguration : IEntityTypeConfiguration<DomainDocument>
{
    public void Configure(EntityTypeBuilder<DomainDocument> builder)
    {
        builder.ToTable("document");

        builder.HasKey(document => document.Id);

        builder.Property(document => document.Id)
            .HasColumnName("id")
            .ValueGeneratedNever()
            .HasConversion(id => id.Value, value => DocumentId.FromGuid(value));

        builder.Property(document => document.Title)
            .HasColumnName("title")
            .IsRequired()
            .HasConversion(title => title.Value, value => DocumentTitle.Create(value));

        builder.Property(document => document.DocType)
            .HasColumnName("doc_type")
            .IsRequired();

        builder.Property(document => document.Status)
            .HasColumnName("status")
            .IsRequired();

        builder.Property(document => document.Sensitivity)
            .HasColumnName("sensitivity")
            .IsRequired()
            .HasDefaultValue("Internal");

        builder.Property(document => document.OwnerId)
            .HasColumnName("owner_id")
            .IsRequired();

        builder.Property(document => document.Department)
            .HasColumnName("department");

        builder.Property(document => document.CreatedBy)
            .HasColumnName("created_by")
            .IsRequired();

        builder.Property(document => document.CreatedAtUtc)
            .HasColumnName("created_at")
            .HasColumnType("timestamptz")
            .HasDefaultValueSql("now()");

        builder.Property(document => document.UpdatedAtUtc)
            .HasColumnName("updated_at")
            .HasColumnType("timestamptz")
            .HasDefaultValueSql("now()");

        builder.Property(document => document.TypeId)
            .HasColumnName("type_id");

        builder.HasOne(document => document.Type)
            .WithMany(type => type.Documents)
            .HasForeignKey(document => document.TypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(document => document.Metadata)
            .WithOne(metadata => metadata.Document)
            .HasForeignKey<DocumentMetadata>(metadata => metadata.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(document => document.Versions)
            .WithOne(version => version.Document)
            .HasForeignKey(version => version.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(document => document.Tags)
            .WithOne(tag => tag.Document)
            .HasForeignKey(tag => tag.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(document => document.SignatureRequests)
            .WithOne(request => request.Document)
            .HasForeignKey(request => request.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(document => document.CreatedBy)
            .HasDatabaseName("IX_document_created_by");

        builder.HasIndex(document => document.DocType)
            .HasDatabaseName("doc_document_type_idx");

        builder.HasIndex(document => document.Status)
            .HasDatabaseName("doc_document_status_idx");

        builder.HasIndex(document => document.OwnerId)
            .HasDatabaseName("doc_document_owner_idx");

        builder.HasIndex(document => document.TypeId)
            .HasDatabaseName("IX_document_type_id");

        builder.Navigation(document => document.Versions)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Navigation(document => document.Tags)
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.Navigation(document => document.SignatureRequests)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
