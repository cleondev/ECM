using System.Text.Json;

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECM.Document.Infrastructure.Migrations;

public partial class InitialDocumentModule : Migration
{
    private static readonly string[] TagLabelNamespacePathColumns = ["namespace_slug", "path"];
    private static readonly string[] VersionDocumentIdVersionNoColumns = ["document_id", "version_no"];

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(
            name: "doc");

        migrationBuilder.CreateTable(
            name: "document_type",
            schema: "doc",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                type_key = table.Column<string>(type: "text", nullable: false),
                type_name = table.Column<string>(type: "text", nullable: false),
                is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_document_type", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "tag_namespace",
            schema: "doc",
            columns: table => new
            {
                namespace_slug = table.Column<string>(type: "text", nullable: false),
                kind = table.Column<string>(type: "text", nullable: false),
                owner_user_id = table.Column<Guid>(type: "uuid", nullable: true),
                display_name = table.Column<string>(type: "text", nullable: true),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_tag_namespace", x => x.namespace_slug);
                table.CheckConstraint("chk_tag_namespace_kind", "kind IN ('system','user')");
            });

        migrationBuilder.CreateTable(
            name: "document",
            schema: "doc",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                title = table.Column<string>(type: "text", nullable: false),
                doc_type = table.Column<string>(type: "text", nullable: false),
                status = table.Column<string>(type: "text", nullable: false),
                sensitivity = table.Column<string>(type: "text", nullable: false, defaultValue: "Internal"),
                owner_id = table.Column<Guid>(type: "uuid", nullable: false),
                department = table.Column<string>(type: "text", nullable: true),
                created_by = table.Column<Guid>(type: "uuid", nullable: false),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                type_id = table.Column<Guid>(type: "uuid", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_document", x => x.id);
                table.ForeignKey(
                    name: "FK_document_document_type_type_id",
                    column: x => x.type_id,
                    principalSchema: "doc",
                    principalTable: "document_type",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_document_created_by_user",
                    column: x => x.created_by,
                    principalSchema: "iam",
                    principalTable: "users",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_document_owner_user",
                    column: x => x.owner_id,
                    principalSchema: "iam",
                    principalTable: "users",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "metadata",
            schema: "doc",
            columns: table => new
            {
                document_id = table.Column<Guid>(type: "uuid", nullable: false),
                data = table.Column<JsonDocument>(type: "jsonb", nullable: false, defaultValueSql: "'{}'::jsonb")
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_metadata", x => x.document_id);
                table.ForeignKey(
                    name: "FK_metadata_document_document_id",
                    column: x => x.document_id,
                    principalSchema: "doc",
                    principalTable: "document",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "tag_label",
            schema: "doc",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                namespace_slug = table.Column<string>(type: "text", nullable: false),
                slug = table.Column<string>(type: "text", nullable: false),
                path = table.Column<string>(type: "text", nullable: false),
                is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                created_by = table.Column<Guid>(type: "uuid", nullable: true),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_tag_label", x => x.id);
                table.ForeignKey(
                    name: "FK_tag_label_tag_namespace_namespace_slug",
                    column: x => x.namespace_slug,
                    principalSchema: "doc",
                    principalTable: "tag_namespace",
                    principalColumn: "namespace_slug",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_tag_label_users_created_by",
                    column: x => x.created_by,
                    principalSchema: "iam",
                    principalTable: "users",
                    principalColumn: "id");
                table.CheckConstraint("chk_tag_path_format", "path ~ '^[a-z0-9_]+(-[a-z0-9_]+)*$'");
            });

        migrationBuilder.CreateTable(
            name: "version",
            schema: "doc",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                document_id = table.Column<Guid>(type: "uuid", nullable: false),
                version_no = table.Column<int>(type: "integer", nullable: false),
                storage_key = table.Column<string>(type: "text", nullable: false),
                bytes = table.Column<long>(type: "bigint", nullable: false),
                mime_type = table.Column<string>(type: "text", nullable: false),
                sha256 = table.Column<string>(type: "text", nullable: false),
                created_by = table.Column<Guid>(type: "uuid", nullable: false),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_version", x => x.id);
                table.ForeignKey(
                    name: "FK_version_document_document_id",
                    column: x => x.document_id,
                    principalSchema: "doc",
                    principalTable: "document",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_version_users_created_by",
                    column: x => x.created_by,
                    principalSchema: "iam",
                    principalTable: "users",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "document_tag",
            schema: "doc",
            columns: table => new
            {
                document_id = table.Column<Guid>(type: "uuid", nullable: false),
                tag_id = table.Column<Guid>(type: "uuid", nullable: false),
                applied_by = table.Column<Guid>(type: "uuid", nullable: true),
                applied_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_document_tag", x => new { x.document_id, x.tag_id });
                table.ForeignKey(
                    name: "FK_document_tag_document_document_id",
                    column: x => x.document_id,
                    principalSchema: "doc",
                    principalTable: "document",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_document_tag_tag_label_tag_id",
                    column: x => x.tag_id,
                    principalSchema: "doc",
                    principalTable: "tag_label",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_document_tag_users_applied_by",
                    column: x => x.applied_by,
                    principalSchema: "iam",
                    principalTable: "users",
                    principalColumn: "id");
            });

        migrationBuilder.CreateTable(
            name: "signature_request",
            schema: "doc",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "uuid_generate_v4()"),
                document_id = table.Column<Guid>(type: "uuid", nullable: false),
                version_id = table.Column<Guid>(type: "uuid", nullable: false),
                provider = table.Column<string>(type: "text", nullable: false),
                request_ref = table.Column<string>(type: "text", nullable: false),
                requested_by = table.Column<Guid>(type: "uuid", nullable: false),
                status = table.Column<string>(type: "text", nullable: false, defaultValue: "pending"),
                payload = table.Column<JsonDocument>(type: "jsonb", nullable: false, defaultValueSql: "'{}'::jsonb"),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_signature_request", x => x.id);
                table.ForeignKey(
                    name: "FK_signature_request_document_document_id",
                    column: x => x.document_id,
                    principalSchema: "doc",
                    principalTable: "document",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_signature_request_version_version_id",
                    column: x => x.version_id,
                    principalSchema: "doc",
                    principalTable: "version",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_signature_request_users_requested_by",
                    column: x => x.requested_by,
                    principalSchema: "iam",
                    principalTable: "users",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "signature_result",
            schema: "doc",
            columns: table => new
            {
                request_id = table.Column<Guid>(type: "uuid", nullable: false),
                status = table.Column<string>(type: "text", nullable: false),
                evidence_hash = table.Column<string>(type: "text", nullable: true),
                evidence_url = table.Column<string>(type: "text", nullable: true),
                received_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                raw_response = table.Column<JsonDocument>(type: "jsonb", nullable: false, defaultValueSql: "'{}'::jsonb")
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_signature_result", x => x.request_id);
                table.ForeignKey(
                    name: "FK_signature_result_signature_request_request_id",
                    column: x => x.request_id,
                    principalSchema: "doc",
                    principalTable: "signature_request",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_document_created_by",
            schema: "doc",
            table: "document",
            column: "created_by");

        migrationBuilder.CreateIndex(
            name: "doc_document_owner_idx",
            schema: "doc",
            table: "document",
            column: "owner_id");

        migrationBuilder.CreateIndex(
            name: "doc_document_status_idx",
            schema: "doc",
            table: "document",
            column: "status");

        migrationBuilder.CreateIndex(
            name: "doc_document_type_idx",
            schema: "doc",
            table: "document",
            column: "doc_type");

        migrationBuilder.CreateIndex(
            name: "IX_document_type_id",
            schema: "doc",
            table: "document",
            column: "type_id");

        migrationBuilder.CreateIndex(
            name: "IX_document_tag_applied_by",
            schema: "doc",
            table: "document_tag",
            column: "applied_by");

        migrationBuilder.CreateIndex(
            name: "IX_document_tag_tag_id",
            schema: "doc",
            table: "document_tag",
            column: "tag_id");

        migrationBuilder.CreateIndex(
            name: "IX_tag_label_created_by",
            schema: "doc",
            table: "tag_label",
            column: "created_by");

        migrationBuilder.CreateIndex(
            name: "tag_label_ns_path_idx",
            schema: "doc",
            table: "tag_label",
            columns: TagLabelNamespacePathColumns,
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_tag_label_namespace_slug",
            schema: "doc",
            table: "tag_label",
            column: "namespace_slug");

        migrationBuilder.CreateIndex(
            name: "IX_signature_request_document_id",
            schema: "doc",
            table: "signature_request",
            column: "document_id");

        migrationBuilder.CreateIndex(
            name: "IX_signature_request_requested_by",
            schema: "doc",
            table: "signature_request",
            column: "requested_by");

        migrationBuilder.CreateIndex(
            name: "IX_signature_request_version_id",
            schema: "doc",
            table: "signature_request",
            column: "version_id");

        migrationBuilder.CreateIndex(
            name: "IX_version_created_by",
            schema: "doc",
            table: "version",
            column: "created_by");

        migrationBuilder.CreateIndex(
            name: "IX_version_document_id_version_no",
            schema: "doc",
            table: "version",
            columns: VersionDocumentIdVersionNoColumns,
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_version_document_id",
            schema: "doc",
            table: "version",
            column: "document_id");

        migrationBuilder.CreateIndex(
            name: "IX_document_type_type_key",
            schema: "doc",
            table: "document_type",
            column: "type_key",
            unique: true);

        migrationBuilder.Sql("CREATE INDEX doc_document_title_fts ON doc.document USING GIN (to_tsvector('simple', coalesce(title, '')));");

        migrationBuilder.Sql("CREATE INDEX doc_metadata_gin ON doc.metadata USING GIN (data jsonb_path_ops);");

        migrationBuilder.Sql("CREATE INDEX tag_label_path_trgm ON doc.tag_label USING GIN (path gin_trgm_ops);");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("DROP INDEX IF EXISTS doc.tag_label_path_trgm;");
        migrationBuilder.Sql("DROP INDEX IF EXISTS doc.doc_metadata_gin;");
        migrationBuilder.Sql("DROP INDEX IF EXISTS doc.doc_document_title_fts;");

        migrationBuilder.DropTable(
            name: "signature_result",
            schema: "doc");

        migrationBuilder.DropTable(
            name: "signature_request",
            schema: "doc");

        migrationBuilder.DropTable(
            name: "document_tag",
            schema: "doc");

        migrationBuilder.DropTable(
            name: "version",
            schema: "doc");

        migrationBuilder.DropTable(
            name: "tag_label",
            schema: "doc");

        migrationBuilder.DropTable(
            name: "metadata",
            schema: "doc");

        migrationBuilder.DropTable(
            name: "document",
            schema: "doc");

        migrationBuilder.DropTable(
            name: "tag_namespace",
            schema: "doc");

        migrationBuilder.DropTable(
            name: "document_type",
            schema: "doc");
    }
}
