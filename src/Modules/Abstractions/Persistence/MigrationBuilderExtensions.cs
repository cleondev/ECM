using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace ECM.Modules.Abstractions.Persistence;

public static class MigrationBuilderExtensions
{
    private const string NpgsqlProviderName = "Npgsql.EntityFrameworkCore.PostgreSQL";

    private const string CreateExtensionsSql = """
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS citext;
CREATE EXTENSION IF NOT EXISTS pg_trgm;
""";

    public static void EnsurePostgresExtensions(this MigrationBuilder migrationBuilder)
    {
        ArgumentNullException.ThrowIfNull(migrationBuilder);

        if (!string.Equals(migrationBuilder.ActiveProvider, NpgsqlProviderName, StringComparison.Ordinal))
        {
            return;
        }

        migrationBuilder.Sql(CreateExtensionsSql);
    }
}
