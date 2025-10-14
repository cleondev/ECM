using ECM.AccessControl.Domain.Relations;
using ECM.AccessControl.Domain.Roles;
using ECM.AccessControl.Domain.Users;
using ECM.AccessControl.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ECM.AccessControl.Infrastructure.Migrations;

[DbContext(typeof(AccessControlDbContext))]
partial class AccessControlDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
        modelBuilder
            .HasDefaultSchema("iam")
            .HasAnnotation("ProductVersion", "8.0.8")
            .HasAnnotation("Relational:MaxIdentifierLength", 63);

        NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

        modelBuilder.Entity<Role>(b =>
        {
            b.Property<Guid>("Id")
                .HasColumnName("id")
                .HasColumnType("uuid")
                .HasDefaultValueSql("uuid_generate_v4()")
                .ValueGeneratedOnAdd();

            b.Property<string>("Name")
                .HasColumnName("name")
                .HasColumnType("text");

            b.HasKey("Id");

            b.HasIndex("Name")
                .IsUnique()
                .HasDatabaseName("ix_roles_name");

            b.ToTable("roles", "iam");
        });

        modelBuilder.Entity<User>(b =>
        {
            b.Property<Guid>("Id")
                .HasColumnName("id")
                .HasColumnType("uuid")
                .HasDefaultValueSql("uuid_generate_v4()")
                .ValueGeneratedOnAdd();

            b.Property<DateTimeOffset>("CreatedAtUtc")
                .HasColumnName("created_at")
                .HasColumnType("timestamp with time zone")
                .HasDefaultValueSql("now()");

            b.Property<string>("Department")
                .HasColumnName("department")
                .HasColumnType("text");

            b.Property<string>("DisplayName")
                .HasColumnName("display_name")
                .HasColumnType("text");

            b.Property<string>("Email")
                .HasColumnName("email")
                .HasColumnType("citext");

            b.Property<bool>("IsActive")
                .HasColumnName("is_active")
                .HasColumnType("boolean")
                .HasDefaultValue(true);

            b.HasKey("Id");

            b.HasIndex("Email")
                .IsUnique()
                .HasDatabaseName("ix_users_email");

            b.ToTable("users", "iam");
        });

        modelBuilder.Entity<UserRole>(b =>
        {
            b.Property<Guid>("UserId")
                .HasColumnName("user_id")
                .HasColumnType("uuid");

            b.Property<Guid>("RoleId")
                .HasColumnName("role_id")
                .HasColumnType("uuid");

            b.HasKey("UserId", "RoleId");

            b.HasIndex("RoleId");

            b.ToTable("user_roles", "iam");

            b.HasOne<User>("User")
                .WithMany("Roles")
                .HasForeignKey("UserId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();

            b.HasOne<Role>("Role")
                .WithMany("UserRoles")
                .HasForeignKey("RoleId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();
        });

        modelBuilder.Entity<AccessRelation>(b =>
        {
            b.Property<Guid>("SubjectId")
                .HasColumnName("subject_id")
                .HasColumnType("uuid");

            b.Property<Guid>("ObjectId")
                .HasColumnName("object_id")
                .HasColumnType("uuid");

            b.Property<string>("ObjectType")
                .HasColumnName("object_type")
                .HasColumnType("text");

            b.Property<string>("Relation")
                .HasColumnName("relation")
                .HasColumnType("text");

            b.Property<DateTimeOffset>("CreatedAtUtc")
                .HasColumnName("created_at")
                .HasColumnType("timestamp with time zone")
                .HasDefaultValueSql("now()");

            b.HasKey("SubjectId", "ObjectType", "ObjectId", "Relation");

            b.HasIndex("ObjectType", "ObjectId")
                .HasDatabaseName("iam_relations_object_idx");

            b.ToTable("relations", "iam");
        });
    }
}
