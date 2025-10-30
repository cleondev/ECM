using ECM.Document.Domain.Tags;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECM.Document.Infrastructure.Persistence.Configurations;

public sealed class TagNamespaceConfiguration : IEntityTypeConfiguration<TagNamespace>
{
    public void Configure(EntityTypeBuilder<TagNamespace> builder)
    {
        builder.ToTable("tag_namespace", tableBuilder =>
        {
            tableBuilder.HasCheckConstraint("chk_tag_namespace_scope", "scope IN ('global','group','user')");
        });

        builder.HasKey(ns => ns.Id);

        builder.Property(ns => ns.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(ns => ns.Scope)
            .HasColumnName("scope")
            .IsRequired();

        builder.Property(ns => ns.OwnerUserId)
            .HasColumnName("owner_user_id");

        builder.Property(ns => ns.OwnerGroupId)
            .HasColumnName("owner_group_id");

        builder.Property(ns => ns.DisplayName)
            .HasColumnName("display_name");

        builder.Property(ns => ns.IsSystem)
            .HasColumnName("is_system")
            .HasDefaultValue(false);

        builder.Property(ns => ns.CreatedAtUtc)
            .HasColumnName("created_at")
            .HasColumnType("timestamptz")
            .HasDefaultValueSql("now()");

        builder.HasIndex(ns => new { ns.Scope, ns.OwnerUserId, ns.OwnerGroupId })
            .HasDatabaseName("tag_namespace_scope_owner_idx");

        builder.Navigation(ns => ns.Labels)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
