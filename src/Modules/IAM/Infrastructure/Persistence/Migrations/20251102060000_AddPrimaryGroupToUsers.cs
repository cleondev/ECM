using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECM.IAM.Infrastructure.Persistence.Migrations;

public partial class AddPrimaryGroupToUsers : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(
            name: "primary_group_id",
            schema: "iam",
            table: "users",
            type: "uuid",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "primary_group_id",
            schema: "iam",
            table: "users");
    }
}
