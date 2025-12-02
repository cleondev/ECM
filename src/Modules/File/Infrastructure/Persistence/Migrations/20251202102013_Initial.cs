using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECM.File.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "file");

            migrationBuilder.CreateTable(
                name: "file_object",
                schema: "file",
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "file_object",
                schema: "file");
        }
    }
}
