using ECM.IAM.Domain.Groups;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECM.IAM.Infrastructure.Persistence.Configurations;

public sealed class GroupMemberConfiguration : IEntityTypeConfiguration<GroupMember>
{
    public void Configure(EntityTypeBuilder<GroupMember> builder)
    {
        builder.ToTable("group_members");

        builder.HasKey(member => new { member.GroupId, member.UserId });

        builder.Property(member => member.GroupId)
            .HasColumnName("group_id");

        builder.Property(member => member.UserId)
            .HasColumnName("user_id");

        builder.Property(member => member.Role)
            .HasColumnName("role")
            .HasDefaultValue(GroupMemberRoles.Member)
            .IsRequired();

        builder.Property(member => member.ValidFromUtc)
            .HasColumnName("valid_from")
            .HasColumnType("timestamptz")
            .HasDefaultValueSql("now()");

        builder.Property(member => member.ValidToUtc)
            .HasColumnName("valid_to")
            .HasColumnType("timestamptz");

        builder.HasIndex(member => new { member.GroupId, member.ValidFromUtc, member.ValidToUtc })
            .HasDatabaseName("iam_group_members_group_validity_idx");
    }
}
