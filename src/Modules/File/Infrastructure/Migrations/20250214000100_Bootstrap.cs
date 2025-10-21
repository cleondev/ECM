using ECM.Modules.Abstractions.Persistence;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECM.File.Infrastructure.Migrations;

public partial class Bootstrap : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsurePostgresExtensions();
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
    }
}
