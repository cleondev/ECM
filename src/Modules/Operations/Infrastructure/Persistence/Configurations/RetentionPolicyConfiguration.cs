using ECM.Operations.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECM.Operations.Infrastructure.Persistence.Configurations;

internal sealed class RetentionPolicyConfiguration : IEntityTypeConfiguration<RetentionPolicy>
{
    public void Configure(EntityTypeBuilder<RetentionPolicy> builder)
    {
        builder.ToTable("retention_policy", "ops");

        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.Id)
            .HasColumnName("id")
            .ValueGeneratedOnAdd()
            .HasDefaultValueSql("uuid_generate_v4()");

        builder.Property(entity => entity.Name)
            .HasColumnName("name")
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(entity => entity.Rule)
            .HasColumnName("rule")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(entity => entity.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true);

        builder.Property(entity => entity.CreatedAtUtc)
            .HasColumnName("created_at")
            .HasDefaultValueSql("now()")
            .IsRequired();
    }
}
