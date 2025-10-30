using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECM.Document.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class addIsValidToEffectiveAclFlat : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "doc_effective_acl_flat_user_document_idx",
                schema: "doc",
                table: "effective_acl_flat");

            migrationBuilder.AddColumn<bool>(
                name: "is_valid",
                schema: "doc",
                table: "effective_acl_flat",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.CreateIndex(
                name: "doc_effective_acl_flat_user_document_idx",
                schema: "doc",
                table: "effective_acl_flat",
                columns: new[] { "user_id", "is_valid", "document_id" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "doc_effective_acl_flat_user_document_idx",
                schema: "doc",
                table: "effective_acl_flat");

            migrationBuilder.DropColumn(
                name: "is_valid",
                schema: "doc",
                table: "effective_acl_flat");

            migrationBuilder.CreateIndex(
                name: "doc_effective_acl_flat_user_document_idx",
                schema: "doc",
                table: "effective_acl_flat",
                columns: new[] { "user_id", "valid_to", "document_id" });
        }
    }
}
