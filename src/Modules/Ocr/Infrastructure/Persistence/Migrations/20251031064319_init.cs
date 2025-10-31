using System;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECM.Ocr.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Init : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "ocr");

            migrationBuilder.CreateTable(
                name: "result",
                schema: "ocr",
                columns: table => new
                {
                    document_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version_id = table.Column<Guid>(type: "uuid", nullable: false),
                    pages = table.Column<int>(type: "integer", nullable: false),
                    lang = table.Column<string>(type: "text", nullable: true),
                    summary = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_result", x => new { x.document_id, x.version_id });
                });

            migrationBuilder.CreateTable(
                name: "template",
                schema: "ocr",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "text", nullable: false),
                    version = table.Column<int>(type: "integer", nullable: false),
                    page_side = table.Column<string>(type: "text", nullable: true),
                    size_ratio = table.Column<string>(type: "text", nullable: true),
                    is_active = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_template", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "extraction",
                schema: "ocr",
                columns: table => new
                {
                    document_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version_id = table.Column<Guid>(type: "uuid", nullable: false),
                    field_key = table.Column<string>(type: "text", nullable: false),
                    value_text = table.Column<string>(type: "text", nullable: true),
                    confidence = table.Column<decimal>(type: "numeric", nullable: true),
                    provenance = table.Column<JsonDocument>(type: "jsonb", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_extraction", x => new { x.document_id, x.version_id, x.field_key });
                    table.ForeignKey(
                        name: "fk_extraction_result_document_id_version_id",
                        columns: x => new { x.document_id, x.version_id },
                        principalSchema: "ocr",
                        principalTable: "result",
                        principalColumns: new[] { "document_id", "version_id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "page_text",
                schema: "ocr",
                columns: table => new
                {
                    document_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version_id = table.Column<Guid>(type: "uuid", nullable: false),
                    page_no = table.Column<int>(type: "integer", nullable: false),
                    content = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_page_text", x => new { x.document_id, x.version_id, x.page_no });
                    table.ForeignKey(
                        name: "fk_page_text_result_document_id_version_id",
                        columns: x => new { x.document_id, x.version_id },
                        principalSchema: "ocr",
                        principalTable: "result",
                        principalColumns: new[] { "document_id", "version_id" },
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "annotation",
                schema: "ocr",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    document_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version_id = table.Column<Guid>(type: "uuid", nullable: false),
                    template_id = table.Column<Guid>(type: "uuid", nullable: true),
                    field_key = table.Column<string>(type: "text", nullable: true),
                    value_text = table.Column<string>(type: "text", nullable: true),
                    bbox_abs = table.Column<JsonDocument>(type: "jsonb", nullable: true),
                    confidence = table.Column<decimal>(type: "numeric", nullable: true),
                    source = table.Column<string>(type: "text", nullable: true),
                    created_by = table.Column<Guid>(type: "uuid", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_annotation", x => x.id);
                    table.ForeignKey(
                        name: "fk_annotation_result_document_id_version_id",
                        columns: x => new { x.document_id, x.version_id },
                        principalSchema: "ocr",
                        principalTable: "result",
                        principalColumns: new[] { "document_id", "version_id" },
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "fk_annotation_templates_template_id",
                        column: x => x.template_id,
                        principalSchema: "ocr",
                        principalTable: "template",
                        principalColumn: "id");
                });

            migrationBuilder.CreateTable(
                name: "field_def",
                schema: "ocr",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    template_id = table.Column<Guid>(type: "uuid", nullable: false),
                    field_key = table.Column<string>(type: "text", nullable: false),
                    bbox_rel = table.Column<JsonDocument>(type: "jsonb", nullable: true),
                    anchor = table.Column<JsonDocument>(type: "jsonb", nullable: true),
                    validator = table.Column<string>(type: "text", nullable: true),
                    required = table.Column<bool>(type: "boolean", nullable: false),
                    order_no = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_field_def", x => x.id);
                    table.ForeignKey(
                        name: "fk_field_def_templates_template_id",
                        column: x => x.template_id,
                        principalSchema: "ocr",
                        principalTable: "template",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_annotation_document_id_version_id",
                schema: "ocr",
                table: "annotation",
                columns: new[] { "document_id", "version_id" });

            migrationBuilder.CreateIndex(
                name: "ix_annotation_template_id",
                schema: "ocr",
                table: "annotation",
                column: "template_id");

            migrationBuilder.CreateIndex(
                name: "ix_field_def_template_id",
                schema: "ocr",
                table: "field_def",
                column: "template_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "annotation",
                schema: "ocr");

            migrationBuilder.DropTable(
                name: "extraction",
                schema: "ocr");

            migrationBuilder.DropTable(
                name: "field_def",
                schema: "ocr");

            migrationBuilder.DropTable(
                name: "page_text",
                schema: "ocr");

            migrationBuilder.DropTable(
                name: "template",
                schema: "ocr");

            migrationBuilder.DropTable(
                name: "result",
                schema: "ocr");
        }
    }
}
