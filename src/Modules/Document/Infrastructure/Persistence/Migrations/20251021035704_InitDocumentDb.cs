using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECM.Document.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitDocumentDb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "doc");

            migrationBuilder.CreateTable(
                name: "document_type",
                schema: "doc",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    type_key = table.Column<string>(type: "text", nullable: false),
                    type_name = table.Column<string>(type: "text", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_document_type", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "file_object",
                schema: "doc",
                columns: table => new
                {
                    storage_key = table.Column<string>(type: "text", nullable: false),
                    legal_hold = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_file_object", x => x.storage_key);
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
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tag_namespace", x => x.namespace_slug);
                    table.CheckConstraint("chk_tag_namespace_kind", "kind IN ('system','user')");
                });

            migrationBuilder.CreateTable(
                name: "document",
                schema: "doc",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    title = table.Column<string>(type: "text", nullable: false),
                    doc_type = table.Column<string>(type: "text", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false),
                    sensitivity = table.Column<string>(type: "text", nullable: false, defaultValue: "Internal"),
                    owner_id = table.Column<Guid>(type: "uuid", nullable: false),
                    department = table.Column<string>(type: "text", nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false, defaultValueSql: "now()"),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false, defaultValueSql: "now()"),
                    type_id = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_document", x => x.id);
                    table.ForeignKey(
                        name: "fk_document_document_types_type_id",
                        column: x => x.type_id,
                        principalSchema: "doc",
                        principalTable: "document_type",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "effective_acl_flat",
                schema: "doc",
                columns: table => new
                {
                    document_id = table.Column<Guid>(type: "uuid", nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    valid_to = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    source = table.Column<string>(type: "text", nullable: false),
                    idempotency_key = table.Column<string>(type: "text", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_effective_acl_flat", x => new { x.document_id, x.user_id, x.idempotency_key });
                    table.ForeignKey(
                        name: "fk_effective_acl_flat_document_document_id",
                        column: x => x.document_id,
                        principalSchema: "doc",
                        principalTable: "document",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_effective_acl_flat_users_user_id",
                        column: x => x.user_id,
                        principalSchema: "iam",
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "tag_label",
                schema: "doc",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    namespace_slug = table.Column<string>(type: "text", nullable: false),
                    slug = table.Column<string>(type: "text", nullable: false),
                    path = table.Column<string>(type: "text", nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_tag_label", x => x.id);
                    table.CheckConstraint("chk_tag_path_format", "path ~ '^[a-z0-9_]+(-[a-z0-9_]+)*$'");
                    table.ForeignKey(
                        name: "fk_tag_label_tag_namespace_namespace_slug",
                        column: x => x.namespace_slug,
                        principalSchema: "doc",
                        principalTable: "tag_namespace",
                        principalColumn: "namespace_slug",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "metadata",
                schema: "doc",
                columns: table => new
                {
                    document_id = table.Column<Guid>(type: "uuid", nullable: false),
                    data = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'{}'::jsonb")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_metadata", x => x.document_id);
                    table.ForeignKey(
                        name: "fk_metadata_document_document_id",
                        column: x => x.document_id,
                        principalSchema: "doc",
                        principalTable: "document",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "version",
                schema: "doc",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    document_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version_no = table.Column<int>(type: "integer", nullable: false),
                    storage_key = table.Column<string>(type: "text", nullable: false),
                    bytes = table.Column<long>(type: "bigint", nullable: false),
                    mime_type = table.Column<string>(type: "text", nullable: false),
                    sha256 = table.Column<string>(type: "text", nullable: false),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_version", x => x.id);
                    table.ForeignKey(
                        name: "fk_version_document_document_id",
                        column: x => x.document_id,
                        principalSchema: "doc",
                        principalTable: "document",
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
                    applied_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_document_tag", x => new { x.document_id, x.tag_id });
                    table.ForeignKey(
                        name: "fk_document_tag_document_document_id",
                        column: x => x.document_id,
                        principalSchema: "doc",
                        principalTable: "document",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_document_tag_tag_labels_tag_id",
                        column: x => x.tag_id,
                        principalSchema: "doc",
                        principalTable: "tag_label",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "signature_request",
                schema: "doc",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    document_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version_id = table.Column<Guid>(type: "uuid", nullable: false),
                    provider = table.Column<string>(type: "text", nullable: false),
                    request_ref = table.Column<string>(type: "text", nullable: false),
                    requested_by = table.Column<Guid>(type: "uuid", nullable: false),
                    status = table.Column<string>(type: "text", nullable: false, defaultValue: "pending"),
                    payload = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'{}'::jsonb"),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false, defaultValueSql: "now()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_signature_request", x => x.id);
                    table.ForeignKey(
                        name: "fk_signature_request_document_document_id",
                        column: x => x.document_id,
                        principalSchema: "doc",
                        principalTable: "document",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_signature_request_version_version_id",
                        column: x => x.version_id,
                        principalSchema: "doc",
                        principalTable: "version",
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
                    received_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false, defaultValueSql: "now()"),
                    raw_response = table.Column<string>(type: "jsonb", nullable: false, defaultValueSql: "'{}'::jsonb")
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_signature_result", x => x.request_id);
                    table.ForeignKey(
                        name: "fk_signature_result_signature_request_request_id",
                        column: x => x.request_id,
                        principalSchema: "doc",
                        principalTable: "signature_request",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

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
                name: "doc_document_updated_at_id_idx",
                schema: "doc",
                table: "document",
                columns: new[] { "updated_at", "id" },
                descending: new[] { true, true });

            migrationBuilder.CreateIndex(
                name: "IX_document_created_by",
                schema: "doc",
                table: "document",
                column: "created_by");

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
                name: "IX_document_type_type_key",
                schema: "doc",
                table: "document_type",
                column: "type_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "doc_effective_acl_flat_user_document_idx",
                schema: "doc",
                table: "effective_acl_flat",
                columns: new[] { "user_id", "valid_to", "document_id" });

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
                name: "IX_tag_label_created_by",
                schema: "doc",
                table: "tag_label",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_tag_label_namespace_slug",
                schema: "doc",
                table: "tag_label",
                column: "namespace_slug");

            migrationBuilder.CreateIndex(
                name: "tag_label_ns_path_idx",
                schema: "doc",
                table: "tag_label",
                columns: ["namespace_slug", "path"],
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_version_created_by",
                schema: "doc",
                table: "version",
                column: "created_by");

            migrationBuilder.CreateIndex(
                name: "IX_version_document_id",
                schema: "doc",
                table: "version",
                column: "document_id");

            migrationBuilder.CreateIndex(
                name: "IX_version_document_id_version_no",
                schema: "doc",
                table: "version",
                columns: ["document_id", "version_no"],
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "document_tag",
                schema: "doc");

            migrationBuilder.DropTable(
                name: "file_object",
                schema: "doc");

            migrationBuilder.DropTable(
                name: "metadata",
                schema: "doc");

            migrationBuilder.DropTable(
                name: "signature_result",
                schema: "doc");

            migrationBuilder.DropTable(
                name: "tag_label",
                schema: "doc");

            migrationBuilder.DropTable(
                name: "signature_request",
                schema: "doc");

            migrationBuilder.DropTable(
                name: "tag_namespace",
                schema: "doc");

            migrationBuilder.DropTable(
                name: "version",
                schema: "doc");

            migrationBuilder.DropTable(
                name: "effective_acl_flat",
                schema: "doc");

            migrationBuilder.DropTable(
                name: "document",
                schema: "doc");

            migrationBuilder.DropTable(
                name: "document_type",
                schema: "doc");
        }
    }
}
