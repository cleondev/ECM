using ECM.File.Infrastructure.Outbox;
using ECM.File.Infrastructure.Persistence;
using ECM.File.Infrastructure.Persistence.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ECM.File.Infrastructure.Migrations;

[DbContext(typeof(FileDbContext))]
partial class FileDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
        modelBuilder
            .HasDefaultSchema("doc")
            .HasAnnotation("ProductVersion", "8.0.8")
            .HasAnnotation("Relational:MaxIdentifierLength", 63);

        NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

        modelBuilder.Entity<StoredFileEntity>(b =>
        {
            b.Property<string>("StorageKey")
                .HasColumnName("storage_key")
                .HasColumnType("text");

            b.Property<DateTimeOffset>("CreatedAtUtc")
                .HasColumnName("created_at")
                .HasColumnType("timestamp with time zone")
                .HasDefaultValueSql("now()");

            b.Property<bool>("LegalHold")
                .HasColumnName("legal_hold")
                .HasColumnType("boolean")
                .HasDefaultValue(false);

            b.HasKey("StorageKey");

            b.ToTable("file_object", "doc");
        });

        modelBuilder.Entity<OutboxMessage>(b =>
        {
            b.Property<long>("Id")
                .HasColumnName("id")
                .HasColumnType("bigint")
                .ValueGeneratedOnAdd();

            b.Property<string>("Aggregate")
                .HasColumnName("aggregate")
                .HasColumnType("character varying(128)")
                .HasMaxLength(128);

            b.Property<Guid>("AggregateId")
                .HasColumnName("aggregate_id")
                .HasColumnType("uuid");

            b.Property<string>("Payload")
                .HasColumnName("payload")
                .HasColumnType("jsonb");

            b.Property<DateTimeOffset?>("ProcessedAtUtc")
                .HasColumnName("processed_at")
                .HasColumnType("timestamp with time zone");

            b.Property<DateTimeOffset>("OccurredAtUtc")
                .HasColumnName("occurred_at")
                .HasColumnType("timestamp with time zone");

            b.Property<string>("Type")
                .HasColumnName("type")
                .HasColumnType("character varying(256)")
                .HasMaxLength(256);

            b.HasKey("Id");

            b.ToTable("outbox", "ops");
        });
    }
}
