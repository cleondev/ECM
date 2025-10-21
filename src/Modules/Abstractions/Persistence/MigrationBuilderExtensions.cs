using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace ECM.Modules.Abstractions.Persistence;

public static class MigrationBuilderExtensions
{
    private const string CreateExtensionsSql = """
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS citext;
CREATE EXTENSION IF NOT EXISTS pg_trgm;
CREATE EXTENSION IF NOT EXISTS btree_gin;
CREATE EXTENSION IF NOT EXISTS vector;
""";

    public static void EnsurePostgresExtensions(this MigrationBuilder migrationBuilder)
    {
        ArgumentNullException.ThrowIfNull(migrationBuilder);

        migrationBuilder.Sql(CreateExtensionsSql);
    }
}
