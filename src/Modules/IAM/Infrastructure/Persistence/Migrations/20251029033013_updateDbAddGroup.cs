using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECM.IAM.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class updateDbAddGroup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {

            migrationBuilder.AddColumn<string>(
                name: "password_hash",
                schema: "iam",
                table: "users",
                type: "text",
                nullable: true);

            migrationBuilder.AddForeignKey(
                name: "fk_group_members_users_user_id",
                schema: "iam",
                table: "group_members",
                column: "user_id",
                principalSchema: "iam",
                principalTable: "users",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "password_hash",
                schema: "iam",
                table: "users");

            migrationBuilder.DropForeignKey(
                name: "fk_group_members_users_user_id",
                schema: "iam",
                table: "group_members");
        }
    }
}
