using ECM.Document.Domain.Tags;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECM.Document.Infrastructure.Persistence.Configurations;

public sealed class TagNamespaceConfiguration : IEntityTypeConfiguration<TagNamespace>
{
    public void Configure(EntityTypeBuilder<TagNamespace> builder)
    {
        builder.ToTable("tag_namespace");

        builder.HasKey(ns => ns.NamespaceSlug);

        builder.Property(ns => ns.NamespaceSlug)
            .HasColumnName("namespace_slug")
            .IsRequired();

        builder.Property(ns => ns.Kind)
            .HasColumnName("kind")
            .IsRequired();

        builder.HasCheckConstraint("chk_tag_namespace_kind", "kind IN ('system','user')");

        builder.Property(ns => ns.OwnerUserId)
            .HasColumnName("owner_user_id");

        builder.Property(ns => ns.DisplayName)
            .HasColumnName("display_name");

        builder.Property(ns => ns.CreatedAtUtc)
            .HasColumnName("created_at")
            .HasColumnType("timestamptz")
            .HasDefaultValueSql("now()");

        builder.Navigation(ns => ns.Labels)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
