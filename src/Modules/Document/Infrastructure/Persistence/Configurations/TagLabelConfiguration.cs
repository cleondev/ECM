using ECM.Document.Domain.Tags;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECM.Document.Infrastructure.Persistence.Configurations;

public sealed class TagLabelConfiguration : IEntityTypeConfiguration<TagLabel>
{
    public void Configure(EntityTypeBuilder<TagLabel> builder)
    {
        builder.ToTable("tag_label");

        builder.HasKey(label => label.Id);

        builder.Property(label => label.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(label => label.NamespaceId)
            .HasColumnName("namespace_id")
            .IsRequired();

        builder.Property(label => label.ParentId)
            .HasColumnName("parent_id");

        builder.Property(label => label.Name)
            .HasColumnName("name")
            .IsRequired();

        builder.Property(label => label.PathIds)
            .HasColumnName("path_ids")
            .HasColumnType("uuid[]")
            .IsRequired();

        builder.Property(label => label.SortOrder)
            .HasColumnName("sort_order")
            .HasDefaultValue(0);

        builder.Property(label => label.Color)
            .HasColumnName("color");

        builder.Property(label => label.IconKey)
            .HasColumnName("icon_key");

        builder.Property(label => label.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true);

        builder.Property(label => label.IsSystem)
            .HasColumnName("is_system")
            .HasDefaultValue(false);

        builder.Property(label => label.CreatedBy)
            .HasColumnName("created_by");

        builder.Property(label => label.CreatedAtUtc)
            .HasColumnName("created_at")
            .HasColumnType("timestamptz")
            .HasDefaultValueSql("now()");

        builder.HasOne(label => label.Namespace)
            .WithMany(ns => ns.Labels)
            .HasForeignKey(label => label.NamespaceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(label => label.Parent)
            .WithMany()
            .HasForeignKey(label => label.ParentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(label => new { label.NamespaceId, label.ParentId, label.Name })
            .IsUnique()
            .HasDatabaseName("uq_tag_sibling_name");

        builder.HasIndex(label => new { label.NamespaceId, label.ParentId })
            .HasDatabaseName("tag_label_ns_parent_idx");

        builder.HasIndex(label => label.PathIds)
            .HasMethod("gin")
            .HasDatabaseName("tag_label_ns_path_gin");

        builder.HasIndex(label => label.Name)
            .HasMethod("gin")
            .HasOperators("public.gin_trgm_ops")
            .HasDatabaseName("tag_label_name_trgm");

        builder.Navigation(label => label.DocumentTags)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
