using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECM.Operations.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RemoveWebhookFromOps : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "webhook_delivery",
                schema: "ops");

            migrationBuilder.DropTable(
                name: "webhook",
                schema: "ops");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "webhook",
                schema: "ops",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    event_types = table.Column<string[]>(type: "text[]", nullable: false),
                    url = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    secret = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    description = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    deactivated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_webhook", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "webhook_delivery",
                schema: "ops",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    webhook_id = table.Column<Guid>(type: "uuid", nullable: false),
                    event_type = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    payload = table.Column<string>(type: "jsonb", nullable: false),
                    status = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    attempt_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    enqueued_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                    delivered_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    error = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_webhook_delivery", x => x.id);
                    table.ForeignKey(
                        name: "fk_webhook_delivery_webhooks_webhook_id",
                        column: x => x.webhook_id,
                        principalSchema: "ops",
                        principalTable: "webhook",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ops_webhook_active_idx",
                schema: "ops",
                table: "webhook",
                column: "is_active");

            migrationBuilder.CreateIndex(
                name: "ops_webhook_delivery_status_idx",
                schema: "ops",
                table: "webhook_delivery",
                columns: new[] { "webhook_id", "status" });
        }
    }
}
