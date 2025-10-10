using ECM.AccessControl.Domain.Relations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ECM.AccessControl.Infrastructure.Persistence.Configurations;

public sealed class AccessRelationConfiguration : IEntityTypeConfiguration<AccessRelation>
{
    public void Configure(EntityTypeBuilder<AccessRelation> builder)
    {
        builder.ToTable("relations");

        builder.HasKey(relation => new
        {
            relation.SubjectId,
            relation.ObjectType,
            relation.ObjectId,
            relation.Relation
        });

        builder.Property(relation => relation.SubjectId)
            .HasColumnName("subject_id");

        builder.Property(relation => relation.ObjectType)
            .HasColumnName("object_type")
            .IsRequired();

        builder.Property(relation => relation.ObjectId)
            .HasColumnName("object_id");

        builder.Property(relation => relation.Relation)
            .HasColumnName("relation")
            .IsRequired();

        builder.Property(relation => relation.CreatedAtUtc)
            .HasColumnName("created_at")
            .HasColumnType("timestamptz")
            .HasDefaultValueSql("now()");

        builder.HasIndex(relation => new { relation.ObjectType, relation.ObjectId })
            .HasDatabaseName("iam_relations_object_idx");
    }
}
