using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace ECM.File.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class updateDB : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "share_link",
                schema: "file",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    code = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    owner_user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    document_id = table.Column<Guid>(type: "uuid", nullable: false),
                    version_id = table.Column<Guid>(type: "uuid", nullable: true),
                    subject_type = table.Column<string>(type: "text", nullable: false),
                    subject_id = table.Column<Guid>(type: "uuid", nullable: true),
                    permissions = table.Column<string[]>(type: "text[]", nullable: false),
                    password_hash = table.Column<string>(type: "text", nullable: true),
                    valid_from = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false, defaultValueSql: "now()"),
                    valid_to = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    max_views = table.Column<int>(type: "integer", nullable: true),
                    max_downloads = table.Column<int>(type: "integer", nullable: true),
                    file_name = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    file_extension = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    file_content_type = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    file_size_bytes = table.Column<long>(type: "bigint", nullable: false),
                    file_created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true),
                    watermark = table.Column<string>(type: "jsonb", nullable: true),
                    allowed_ips = table.Column<string[]>(type: "text[]", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false, defaultValueSql: "now()"),
                    revoked_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_share_link", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "share_access_event",
                schema: "file",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    share_id = table.Column<Guid>(type: "uuid", nullable: false),
                    occurred_at = table.Column<DateTimeOffset>(type: "timestamptz", nullable: false, defaultValueSql: "now()"),
                    action = table.Column<string>(type: "text", nullable: false),
                    remote_ip = table.Column<string>(type: "text", nullable: true),
                    user_agent = table.Column<string>(type: "text", nullable: true),
                    ok = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_share_access_event", x => x.id);
                    table.ForeignKey(
                        name: "fk_share_access_event_share_links_share_id",
                        column: x => x.share_id,
                        principalSchema: "file",
                        principalTable: "share_link",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_share_access_event_share_id",
                schema: "file",
                table: "share_access_event",
                column: "share_id");

            migrationBuilder.CreateIndex(
                name: "ix_share_link_code",
                schema: "file",
                table: "share_link",
                column: "code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_share_link_doc",
                schema: "file",
                table: "share_link",
                columns: new[] { "document_id", "version_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "share_access_event",
                schema: "file");

            migrationBuilder.DropTable(
                name: "share_link",
                schema: "file");
        }
    }
}
