using ECM.File.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECM.File.Infrastructure.Persistence.Configurations;

public sealed class ShareLinkEntityConfiguration : IEntityTypeConfiguration<ShareLinkEntity>
{
    public void Configure(EntityTypeBuilder<ShareLinkEntity> builder)
    {
        builder.ToTable("share_link");

        builder.HasKey(link => link.Id);

        builder.Property(link => link.Id)
            .HasColumnName("id");

        builder.Property(link => link.Code)
            .HasColumnName("code")
            .HasMaxLength(16)
            .IsRequired();

        builder.Property(link => link.OwnerUserId)
            .HasColumnName("owner_user_id")
            .IsRequired();

        builder.Property(link => link.DocumentId)
            .HasColumnName("document_id")
            .IsRequired();

        builder.Property(link => link.VersionId)
            .HasColumnName("version_id");

        builder.Property(link => link.SubjectType)
            .HasColumnName("subject_type")
            .HasColumnType("share_subject")
            .IsRequired();

        builder.Property(link => link.SubjectId)
            .HasColumnName("subject_id");

        builder.Property(link => link.Permissions)
            .HasColumnName("permissions")
            .HasColumnType("share_perm[]")
            .HasConversion(
                value => value,
                value => value ?? Array.Empty<string>());

        builder.Property(link => link.PasswordHash)
            .HasColumnName("password_hash");

        builder.Property(link => link.ValidFrom)
            .HasColumnName("valid_from")
            .HasColumnType("timestamptz")
            .HasDefaultValueSql("now()");

        builder.Property(link => link.ValidTo)
            .HasColumnName("valid_to")
            .HasColumnType("timestamptz");

        builder.Property(link => link.MaxViews)
            .HasColumnName("max_views");

        builder.Property(link => link.MaxDownloads)
            .HasColumnName("max_downloads");

        builder.Property(link => link.FileName)
            .HasColumnName("file_name")
            .HasMaxLength(512)
            .IsRequired();

        builder.Property(link => link.FileExtension)
            .HasColumnName("file_extension")
            .HasMaxLength(32);

        builder.Property(link => link.FileContentType)
            .HasColumnName("file_content_type")
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(link => link.FileSizeBytes)
            .HasColumnName("file_size_bytes")
            .HasColumnType("bigint")
            .IsRequired();

        builder.Property(link => link.FileCreatedAt)
            .HasColumnName("file_created_at")
            .HasColumnType("timestamptz");

        builder.Property(link => link.WatermarkJson)
            .HasColumnName("watermark")
            .HasColumnType("jsonb");

        builder.Property(link => link.AllowedIps)
            .HasColumnName("allowed_ips")
            .HasColumnType("text[]")
            .HasConversion(
                value => value,
                value => value ?? Array.Empty<string>());

        builder.Property(link => link.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamptz")
            .HasDefaultValueSql("now()");

        builder.Property(link => link.RevokedAt)
            .HasColumnName("revoked_at")
            .HasColumnType("timestamptz");

        builder.HasIndex(link => link.Code)
            .IsUnique();

        builder.HasIndex(link => new { link.DocumentId, link.VersionId })
            .HasDatabaseName("ix_share_link_doc");
    }
}
