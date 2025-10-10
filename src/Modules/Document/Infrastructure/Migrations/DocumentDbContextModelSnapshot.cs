using System;
using System.Text.Json;
using ECM.Modules.Document.Domain.Documents;
using ECM.Modules.Document.Domain.DocumentTypes;
using ECM.Modules.Document.Domain.Files;
using ECM.Modules.Document.Domain.Signatures;
using ECM.Modules.Document.Domain.Tags;
using ECM.Modules.Document.Domain.Versions;
using ECM.Modules.Document.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;
using DocumentAggregate = ECM.Modules.Document.Domain.Documents.Document;

#nullable disable

namespace ECM.Modules.Document.Infrastructure.Migrations;

[DbContext(typeof(DocumentDbContext))]
partial class DocumentDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
#pragma warning disable 612, 618
        modelBuilder
            .HasDefaultSchema("doc")
            .HasAnnotation("ProductVersion", "8.0.6")
            .HasAnnotation("Relational:MaxIdentifierLength", 63);

        NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

        var documentIdConverter = new ValueConverter<DocumentId, Guid>(value => value.Value, value => DocumentId.FromGuid(value));
        var documentTitleConverter = new ValueConverter<DocumentTitle, string>(value => value.Value, value => DocumentTitle.Create(value));

        modelBuilder.Entity<DocumentAggregate>(b =>
        {
            b.Property<DocumentId>("Id")
                .HasColumnName("id")
                .HasColumnType("uuid")
                .HasConversion(documentIdConverter)
                .HasDefaultValueSql("uuid_generate_v4()")
                .ValueGeneratedOnAdd();

            b.Property<Guid>("CreatedBy")
                .HasColumnName("created_by")
                .HasColumnType("uuid");

            b.Property<DateTimeOffset>("CreatedAtUtc")
                .HasColumnName("created_at")
                .HasColumnType("timestamp with time zone")
                .HasDefaultValueSql("now()");

            b.Property<string>("Department")
                .HasColumnName("department")
                .HasColumnType("text");

            b.Property<string>("DocType")
                .HasColumnName("doc_type")
                .HasColumnType("text");

            b.Property<Guid>("OwnerId")
                .HasColumnName("owner_id")
                .HasColumnType("uuid");

            b.Property<string>("Sensitivity")
                .HasColumnName("sensitivity")
                .HasColumnType("text")
                .HasDefaultValue("Internal");

            b.Property<string>("Status")
                .HasColumnName("status")
                .HasColumnType("text");

            b.Property<DocumentTitle>("Title")
                .HasColumnName("title")
                .HasColumnType("text")
                .HasConversion(documentTitleConverter);

            b.Property<Guid?>("TypeId")
                .HasColumnName("type_id")
                .HasColumnType("uuid");

            b.Property<DateTimeOffset>("UpdatedAtUtc")
                .HasColumnName("updated_at")
                .HasColumnType("timestamp with time zone")
                .HasDefaultValueSql("now()");

            b.HasKey("Id");

            b.HasIndex("CreatedBy")
                .HasDatabaseName("IX_document_created_by");

            b.HasIndex("DocType")
                .HasDatabaseName("doc_document_type_idx");

            b.HasIndex("OwnerId")
                .HasDatabaseName("doc_document_owner_idx");

            b.HasIndex("Status")
                .HasDatabaseName("doc_document_status_idx");

            b.HasIndex("TypeId")
                .HasDatabaseName("IX_document_type_id");

            b.ToTable("document", "doc");

            b.HasOne(d => d.Type)
                .WithMany(t => t.Documents)
                .HasForeignKey("TypeId")
                .OnDelete(DeleteBehavior.Restrict)
                .HasConstraintName("FK_document_document_type_type_id");

            b.HasOne(d => d.Metadata)
                .WithOne(m => m.Document)
                .HasForeignKey<DocumentMetadata>(m => m.DocumentId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_metadata_document_document_id");

            b.HasMany(d => d.Versions)
                .WithOne(v => v.Document)
                .HasForeignKey(v => v.DocumentId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_version_document_document_id");

            b.HasMany(d => d.Tags)
                .WithOne(t => t.Document)
                .HasForeignKey(t => t.DocumentId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_document_tag_document_document_id");

            b.HasMany(d => d.SignatureRequests)
                .WithOne(r => r.Document)
                .HasForeignKey(r => r.DocumentId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_signature_request_document_document_id");

            b.Navigation(d => d.Metadata);
            b.Navigation(d => d.SignatureRequests)
                .UsePropertyAccessMode(PropertyAccessMode.Field);
            b.Navigation(d => d.Tags)
                .UsePropertyAccessMode(PropertyAccessMode.Field);
            b.Navigation(d => d.Type);
            b.Navigation(d => d.Versions)
                .UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity<DocumentMetadata>(b =>
        {
            b.Property<DocumentId>("DocumentId")
                .HasColumnName("document_id")
                .HasColumnType("uuid")
                .HasConversion(documentIdConverter);

            b.Property<JsonDocument>("Data")
                .HasColumnName("data")
                .HasColumnType("jsonb")
                .HasDefaultValueSql("'{}'::jsonb");

            b.HasKey("DocumentId");

            b.ToTable("metadata", "doc");
        });

        modelBuilder.Entity<DocumentTag>(b =>
        {
            b.Property<DocumentId>("DocumentId")
                .HasColumnName("document_id")
                .HasColumnType("uuid")
                .HasConversion(documentIdConverter);

            b.Property<Guid>("TagId")
                .HasColumnName("tag_id")
                .HasColumnType("uuid");

            b.Property<Guid?>("AppliedBy")
                .HasColumnName("applied_by")
                .HasColumnType("uuid");

            b.Property<DateTimeOffset>("AppliedAtUtc")
                .HasColumnName("applied_at")
                .HasColumnType("timestamp with time zone")
                .HasDefaultValueSql("now()");

            b.HasKey("DocumentId", "TagId");

            b.HasIndex("AppliedBy")
                .HasDatabaseName("IX_document_tag_applied_by");

            b.HasIndex("TagId")
                .HasDatabaseName("IX_document_tag_tag_id");

            b.ToTable("document_tag", "doc");

            b.HasOne(dt => dt.Document)
                .WithMany(d => d.Tags)
                .HasForeignKey(dt => dt.DocumentId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_document_tag_document_document_id");

            b.HasOne(dt => dt.Tag)
                .WithMany(t => t.DocumentTags)
                .HasForeignKey(dt => dt.TagId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_document_tag_tag_label_tag_id");
        });

        modelBuilder.Entity<DocumentType>(b =>
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

            b.Property<bool>("IsActive")
                .HasColumnName("is_active")
                .HasColumnType("boolean")
                .HasDefaultValue(true);

            b.Property<string>("TypeKey")
                .HasColumnName("type_key")
                .HasColumnType("text");

            b.Property<string>("TypeName")
                .HasColumnName("type_name")
                .HasColumnType("text");

            b.HasKey("Id");

            b.HasIndex("TypeKey")
                .HasDatabaseName("IX_document_type_type_key")
                .IsUnique();

            b.ToTable("document_type", "doc");

            b.Navigation(dt => dt.Documents)
                .UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity<DocumentVersion>(b =>
        {
            b.Property<Guid>("Id")
                .HasColumnName("id")
                .HasColumnType("uuid")
                .HasDefaultValueSql("uuid_generate_v4()")
                .ValueGeneratedOnAdd();

            b.Property<long>("Bytes")
                .HasColumnName("bytes")
                .HasColumnType("bigint");

            b.Property<Guid>("CreatedBy")
                .HasColumnName("created_by")
                .HasColumnType("uuid");

            b.Property<DateTimeOffset>("CreatedAtUtc")
                .HasColumnName("created_at")
                .HasColumnType("timestamp with time zone")
                .HasDefaultValueSql("now()");

            b.Property<DocumentId>("DocumentId")
                .HasColumnName("document_id")
                .HasColumnType("uuid")
                .HasConversion(documentIdConverter);

            b.Property<string>("MimeType")
                .HasColumnName("mime_type")
                .HasColumnType("text");

            b.Property<string>("Sha256")
                .HasColumnName("sha256")
                .HasColumnType("text");

            b.Property<string>("StorageKey")
                .HasColumnName("storage_key")
                .HasColumnType("text");

            b.Property<int>("VersionNo")
                .HasColumnName("version_no")
                .HasColumnType("integer");

            b.HasKey("Id");

            b.HasIndex("CreatedBy")
                .HasDatabaseName("IX_version_created_by");

            b.HasIndex("DocumentId")
                .HasDatabaseName("IX_version_document_id");

            b.HasIndex("DocumentId", "VersionNo")
                .HasDatabaseName("IX_version_document_id_version_no")
                .IsUnique();

            b.ToTable("version", "doc");

            b.HasOne(v => v.Document)
                .WithMany(d => d.Versions)
                .HasForeignKey(v => v.DocumentId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_version_document_document_id");

            b.Navigation(v => v.SignatureRequests)
                .UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity<FileObject>(b =>
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

        modelBuilder.Entity<SignatureRequest>(b =>
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

            b.Property<DocumentId>("DocumentId")
                .HasColumnName("document_id")
                .HasColumnType("uuid")
                .HasConversion(documentIdConverter);

            b.Property<JsonDocument>("Payload")
                .HasColumnName("payload")
                .HasColumnType("jsonb")
                .HasDefaultValueSql("'{}'::jsonb");

            b.Property<string>("Provider")
                .HasColumnName("provider")
                .HasColumnType("text");

            b.Property<Guid>("RequestedBy")
                .HasColumnName("requested_by")
                .HasColumnType("uuid");

            b.Property<string>("RequestReference")
                .HasColumnName("request_ref")
                .HasColumnType("text");

            b.Property<string>("Status")
                .HasColumnName("status")
                .HasColumnType("text")
                .HasDefaultValue("pending");

            b.Property<Guid>("VersionId")
                .HasColumnName("version_id")
                .HasColumnType("uuid");

            b.HasKey("Id");

            b.HasIndex("DocumentId")
                .HasDatabaseName("IX_signature_request_document_id");

            b.HasIndex("RequestedBy")
                .HasDatabaseName("IX_signature_request_requested_by");

            b.HasIndex("VersionId")
                .HasDatabaseName("IX_signature_request_version_id");

            b.ToTable("signature_request", "doc");

            b.HasOne(r => r.Document)
                .WithMany(d => d.SignatureRequests)
                .HasForeignKey(r => r.DocumentId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_signature_request_document_document_id");

            b.HasOne(r => r.Version)
                .WithMany(v => v.SignatureRequests)
                .HasForeignKey(r => r.VersionId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_signature_request_version_version_id");

            b.Navigation(r => r.Result);
        });

        modelBuilder.Entity<SignatureResult>(b =>
        {
            b.Property<Guid>("RequestId")
                .HasColumnName("request_id")
                .HasColumnType("uuid");

            b.Property<string>("EvidenceHash")
                .HasColumnName("evidence_hash")
                .HasColumnType("text");

            b.Property<string>("EvidenceUrl")
                .HasColumnName("evidence_url")
                .HasColumnType("text");

            b.Property<JsonDocument>("RawResponse")
                .HasColumnName("raw_response")
                .HasColumnType("jsonb")
                .HasDefaultValueSql("'{}'::jsonb");

            b.Property<DateTimeOffset>("ReceivedAtUtc")
                .HasColumnName("received_at")
                .HasColumnType("timestamp with time zone")
                .HasDefaultValueSql("now()");

            b.Property<string>("Status")
                .HasColumnName("status")
                .HasColumnType("text");

            b.HasKey("RequestId");

            b.ToTable("signature_result", "doc");

            b.HasOne(r => r.Request)
                .WithOne(s => s.Result)
                .HasForeignKey<SignatureResult>(r => r.RequestId)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_signature_result_signature_request_request_id");
        });

        modelBuilder.Entity<TagLabel>(b =>
        {
            b.Property<Guid>("Id")
                .HasColumnName("id")
                .HasColumnType("uuid")
                .HasDefaultValueSql("uuid_generate_v4()")
                .ValueGeneratedOnAdd();

            b.Property<Guid?>("CreatedBy")
                .HasColumnName("created_by")
                .HasColumnType("uuid");

            b.Property<DateTimeOffset>("CreatedAtUtc")
                .HasColumnName("created_at")
                .HasColumnType("timestamp with time zone")
                .HasDefaultValueSql("now()");

            b.Property<bool>("IsActive")
                .HasColumnName("is_active")
                .HasColumnType("boolean")
                .HasDefaultValue(true);

            b.Property<string>("NamespaceSlug")
                .HasColumnName("namespace_slug")
                .HasColumnType("text");

            b.Property<string>("Path")
                .HasColumnName("path")
                .HasColumnType("text");

            b.Property<string>("Slug")
                .HasColumnName("slug")
                .HasColumnType("text");

            b.HasKey("Id");

            b.HasIndex("CreatedBy")
                .HasDatabaseName("IX_tag_label_created_by");

            b.HasIndex("NamespaceSlug")
                .HasDatabaseName("IX_tag_label_namespace_slug");

            b.HasIndex("NamespaceSlug", "Path")
                .HasDatabaseName("tag_label_ns_path_idx")
                .IsUnique();

            b.HasCheckConstraint("chk_tag_path_format", "path ~ '^[a-z0-9_]+(-[a-z0-9_]+)*$'");

            b.ToTable("tag_label", "doc");

            b.HasOne(l => l.Namespace)
                .WithMany(n => n.Labels)
                .HasForeignKey(l => l.NamespaceSlug)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("FK_tag_label_tag_namespace_namespace_slug");

            b.Navigation(l => l.DocumentTags)
                .UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity<TagNamespace>(b =>
        {
            b.Property<string>("NamespaceSlug")
                .HasColumnName("namespace_slug")
                .HasColumnType("text");

            b.Property<DateTimeOffset>("CreatedAtUtc")
                .HasColumnName("created_at")
                .HasColumnType("timestamp with time zone")
                .HasDefaultValueSql("now()");

            b.Property<string>("DisplayName")
                .HasColumnName("display_name")
                .HasColumnType("text");

            b.Property<string>("Kind")
                .HasColumnName("kind")
                .HasColumnType("text");

            b.Property<Guid?>("OwnerUserId")
                .HasColumnName("owner_user_id")
                .HasColumnType("uuid");

            b.HasKey("NamespaceSlug");

            b.HasCheckConstraint("chk_tag_namespace_kind", "kind IN ('system','user')");

            b.ToTable("tag_namespace", "doc");

            b.Navigation(n => n.Labels)
                .UsePropertyAccessMode(PropertyAccessMode.Field);
        });
#pragma warning restore 612, 618
    }
}
