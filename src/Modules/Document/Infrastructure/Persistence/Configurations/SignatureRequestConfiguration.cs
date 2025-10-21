using ECM.Document.Domain.Documents;
using ECM.Document.Domain.Signatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECM.Document.Infrastructure.Persistence.Configurations;

public sealed class SignatureRequestConfiguration : IEntityTypeConfiguration<SignatureRequest>
{
    public void Configure(EntityTypeBuilder<SignatureRequest> builder)
    {
        builder.ToTable("signature_request");

        builder.HasKey(request => request.Id);

        builder.Property(request => request.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(request => request.DocumentId)
            .HasColumnName("document_id")
            .HasConversion(id => id.Value, value => DocumentId.FromGuid(value))
            .IsRequired();

        builder.Property(request => request.VersionId)
            .HasColumnName("version_id")
            .IsRequired();

        builder.Property(request => request.Provider)
            .HasColumnName("provider")
            .IsRequired();

        builder.Property(request => request.RequestReference)
            .HasColumnName("request_ref")
            .IsRequired();

        builder.Property(request => request.RequestedBy)
            .HasColumnName("requested_by")
            .IsRequired();

        builder.Property(request => request.Status)
            .HasColumnName("status")
            .IsRequired()
            .HasDefaultValue("pending");

        builder.Property(request => request.Payload)
            .HasColumnName("payload")
            .ConfigureJsonDocument();

        builder.Property(request => request.CreatedAtUtc)
            .HasColumnName("created_at")
            .HasColumnType("timestamptz")
            .HasDefaultValueSql("now()");

        builder.HasIndex(request => request.DocumentId)
            .HasDatabaseName("IX_signature_request_document_id");

        builder.HasIndex(request => request.RequestedBy)
            .HasDatabaseName("IX_signature_request_requested_by");

        builder.HasIndex(request => request.VersionId)
            .HasDatabaseName("IX_signature_request_version_id");

        builder.HasOne(request => request.Document)
            .WithMany(document => document.SignatureRequests)
            .HasForeignKey(request => request.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(request => request.Version)
            .WithMany(version => version.SignatureRequests)
            .HasForeignKey(request => request.VersionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
