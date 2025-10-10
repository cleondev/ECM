using ECM.Modules.Document.Domain.Tags;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECM.Modules.Document.Infrastructure.Persistence.Configurations;

public sealed class TagLabelConfiguration : IEntityTypeConfiguration<TagLabel>
{
    public void Configure(EntityTypeBuilder<TagLabel> builder)
    {
        builder.ToTable("tag_label");

        builder.HasKey(label => label.Id);

        builder.Property(label => label.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd()
            .HasDefaultValueSql("uuid_generate_v4()");

        builder.Property(label => label.NamespaceSlug)
            .HasColumnName("namespace_slug")
            .IsRequired();

        builder.Property(label => label.Slug)
            .HasColumnName("slug")
            .IsRequired();

        builder.Property(label => label.Path)
            .HasColumnName("path")
            .IsRequired();

        builder.Property(label => label.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true);

        builder.Property(label => label.CreatedBy)
            .HasColumnName("created_by");

        builder.Property(label => label.CreatedAtUtc)
            .HasColumnName("created_at")
            .HasColumnType("timestamptz")
            .HasDefaultValueSql("now()");

        builder.HasOne(label => label.Namespace)
            .WithMany(ns => ns.Labels)
            .HasForeignKey(label => label.NamespaceSlug)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(label => label.CreatedBy)
            .HasDatabaseName("IX_tag_label_created_by");

        builder.HasIndex(label => label.NamespaceSlug)
            .HasDatabaseName("IX_tag_label_namespace_slug");

        builder.HasIndex(label => new { label.NamespaceSlug, label.Path })
            .HasDatabaseName("tag_label_ns_path_idx")
            .IsUnique();

        builder.HasCheckConstraint("chk_tag_path_format", "path ~ '^[a-z0-9_]+(-[a-z0-9_]+)*$'");

        builder.Navigation(label => label.DocumentTags)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
