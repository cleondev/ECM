using ECM.IAM.Domain.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECM.IAM.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(user => user.Id);

        builder.Property(user => user.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(user => user.Email)
            .HasColumnName("email")
            .IsRequired();

        builder.Property(user => user.DisplayName)
            .HasColumnName("display_name")
            .IsRequired();

        builder.Property(user => user.Department)
            .HasColumnName("department");

        builder.Property(user => user.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true);

        builder.Property(user => user.CreatedAtUtc)
            .HasColumnName("created_at")
            .HasColumnType("timestamptz")
            .HasDefaultValueSql("now()");

        builder.HasIndex(user => user.Email)
            .IsUnique()
            .HasDatabaseName("ix_users_email");

        builder.Navigation(user => user.Roles)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
