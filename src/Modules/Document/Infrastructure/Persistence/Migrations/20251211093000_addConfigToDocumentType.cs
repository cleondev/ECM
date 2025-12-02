using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECM.Document.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class addConfigToDocumentType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "config",
                schema: "doc",
                table: "document_type",
                type: "jsonb",
                nullable: false,
                defaultValueSql: "'{}'::jsonb");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "config",
                schema: "doc",
                table: "document_type");
        }
    }
}
