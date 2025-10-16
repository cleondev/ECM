using ECM.IAM.Domain.Roles;
using ECM.IAM.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECM.IAM.Infrastructure.Persistence.Configurations;

public sealed class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> builder)
    {
        builder.ToTable("user_roles");

        builder.HasKey(link => new { link.UserId, link.RoleId });

        builder.Property(link => link.UserId)
            .HasColumnName("user_id");

        builder.Property(link => link.RoleId)
            .HasColumnName("role_id");

        builder.HasOne(link => link.User)
            .WithMany(user => user.Roles)
            .HasForeignKey(link => link.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(link => link.Role)
            .WithMany(role => role.UserRoles)
            .HasForeignKey(link => link.RoleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
