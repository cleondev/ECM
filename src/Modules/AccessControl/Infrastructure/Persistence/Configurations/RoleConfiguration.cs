using ECM.AccessControl.Domain.Roles;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECM.AccessControl.Infrastructure.Persistence.Configurations;

public sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("roles");

        builder.HasKey(role => role.Id);

        builder.Property(role => role.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd()
            .HasDefaultValueSql("uuid_generate_v4()");

        builder.Property(role => role.Name)
            .HasColumnName("name")
            .IsRequired();

        builder.HasIndex(role => role.Name)
            .IsUnique()
            .HasDatabaseName("ix_roles_name");

        builder.Navigation(role => role.UserRoles)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
