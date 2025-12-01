using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECM.Webhook.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWebhookDeliveryDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_webhook_delivery_status",
                schema: "webhook",
                table: "webhook_delivery");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "last_attempt_at",
                schema: "webhook",
                table: "webhook_delivery",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone");

            migrationBuilder.AddColumn<string>(
                name: "correlation_id",
                schema: "webhook",
                table: "webhook_delivery",
                type: "character varying(128)",
                maxLength: 128,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "created_at",
                schema: "webhook",
                table: "webhook_delivery",
                type: "timestamp with time zone",
                nullable: false,
                defaultValueSql: "NOW()");

            migrationBuilder.AddColumn<string>(
                name: "last_error",
                schema: "webhook",
                table: "webhook_delivery",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "payload",
                schema: "webhook",
                table: "webhook_delivery",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'{}'::jsonb");

            migrationBuilder.CreateIndex(
                name: "ix_webhook_delivery_created",
                schema: "webhook",
                table: "webhook_delivery",
                column: "created_at");

            migrationBuilder.CreateIndex(
                name: "ix_webhook_delivery_endpoint_status",
                schema: "webhook",
                table: "webhook_delivery",
                columns: new[] { "endpoint_key", "status" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "ix_webhook_delivery_created",
                schema: "webhook",
                table: "webhook_delivery");

            migrationBuilder.DropIndex(
                name: "ix_webhook_delivery_endpoint_status",
                schema: "webhook",
                table: "webhook_delivery");

            migrationBuilder.DropColumn(
                name: "correlation_id",
                schema: "webhook",
                table: "webhook_delivery");

            migrationBuilder.DropColumn(
                name: "created_at",
                schema: "webhook",
                table: "webhook_delivery");

            migrationBuilder.DropColumn(
                name: "last_error",
                schema: "webhook",
                table: "webhook_delivery");

            migrationBuilder.DropColumn(
                name: "payload",
                schema: "webhook",
                table: "webhook_delivery");

            migrationBuilder.AlterColumn<DateTimeOffset>(
                name: "last_attempt_at",
                schema: "webhook",
                table: "webhook_delivery",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTimeOffset),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_webhook_delivery_status",
                schema: "webhook",
                table: "webhook_delivery",
                column: "status");
        }
    }
}
