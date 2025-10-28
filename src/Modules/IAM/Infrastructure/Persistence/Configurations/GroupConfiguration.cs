using ECM.IAM.Domain.Groups;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECM.IAM.Infrastructure.Persistence.Configurations;

public sealed class GroupConfiguration : IEntityTypeConfiguration<Group>
{
    public void Configure(EntityTypeBuilder<Group> builder)
    {
        builder.ToTable("groups");

        builder.HasKey(group => group.Id);

        builder.Property(group => group.Id)
            .HasColumnName("id")
            .ValueGeneratedNever();

        builder.Property(group => group.Name)
            .HasColumnName("name")
            .IsRequired();

        builder.Property(group => group.Kind)
            .HasColumnName("kind")
            .HasDefaultValue("normal")
            .IsRequired();

        builder.Property(group => group.CreatedBy)
            .HasColumnName("created_by");

        builder.Property(group => group.CreatedAtUtc)
            .HasColumnName("created_at")
            .HasColumnType("timestamptz")
            .HasDefaultValueSql("now()");

        builder.HasMany(group => group.Members)
            .WithOne(member => member.Group)
            .HasForeignKey(member => member.GroupId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(group => group.Name)
            .IsUnique();
    }
}
