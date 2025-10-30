using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECM.IAM.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class updateDBAddGroup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "department",
                schema: "iam",
                table: "users");

            migrationBuilder.AddColumn<Guid>(
                name: "primary_group_id",
                schema: "iam",
                table: "users",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "ix_group_members_user_id",
                schema: "iam",
                table: "group_members",
                column: "user_id");

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
            migrationBuilder.DropForeignKey(
                name: "fk_group_members_users_user_id",
                schema: "iam",
                table: "group_members");

            migrationBuilder.DropIndex(
                name: "ix_group_members_user_id",
                schema: "iam",
                table: "group_members");

            migrationBuilder.DropColumn(
                name: "primary_group_id",
                schema: "iam",
                table: "users");

            migrationBuilder.AddColumn<string>(
                name: "department",
                schema: "iam",
                table: "users",
                type: "text",
                nullable: true);
        }
    }
}
