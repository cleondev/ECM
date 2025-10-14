using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECM.File.Infrastructure.Migrations;

public partial class InitialFileModule : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(
            name: "doc");

        migrationBuilder.EnsureSchema(
            name: "ops");

        migrationBuilder.CreateTable(
            name: "file_object",
            schema: "doc",
            columns: table => new
            {
                storage_key = table.Column<string>(type: "text", nullable: false),
                legal_hold = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()")
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_file_object", x => x.storage_key);
            });

        migrationBuilder.CreateTable(
            name: "outbox",
            schema: "ops",
            columns: table => new
            {
                id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", Npgsql.EntityFrameworkCore.PostgreSQL.Metadata.NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                aggregate = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                aggregate_id = table.Column<Guid>(type: "uuid", nullable: false),
                type = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                payload = table.Column<string>(type: "jsonb", nullable: false),
                occurred_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                processed_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_outbox", x => x.id);
            });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "outbox",
            schema: "ops");

        migrationBuilder.DropTable(
            name: "file_object",
            schema: "doc");
    }
}
