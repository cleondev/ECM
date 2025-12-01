using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECM.Webhook.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitWebhookDb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "webhook");

            migrationBuilder.CreateTable(
                name: "webhook_delivery",
                schema: "webhook",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    request_id = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    endpoint_key = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    attempt_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false, defaultValue: "Pending"),
                    last_attempt_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_webhook_delivery", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_webhook_delivery_status",
                schema: "webhook",
                table: "webhook_delivery",
                column: "status");

            migrationBuilder.CreateIndex(
                name: "ux_webhook_delivery_request_endpoint",
                schema: "webhook",
                table: "webhook_delivery",
                columns: new[] { "request_id", "endpoint_key" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "webhook_delivery",
                schema: "webhook");
        }
    }
}
