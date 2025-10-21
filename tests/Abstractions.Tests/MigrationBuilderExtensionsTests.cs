using System.Linq;
using ECM.Modules.Abstractions.Persistence;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Xunit;

namespace Abstractions.Tests;

public class MigrationBuilderExtensionsTests
{
    [Fact]
    public void EnsurePostgresExtensions_SkipsWhenActiveProviderIsNotNpgsql()
    {
        var migrationBuilder = new MigrationBuilder("Microsoft.EntityFrameworkCore.SqlServer");

        migrationBuilder.EnsurePostgresExtensions();

        Assert.Empty(migrationBuilder.Operations);
    }

    [Fact]
    public void EnsurePostgresExtensions_AppendsSqlOperation_WhenActiveProviderIsNpgsql()
    {
        var migrationBuilder = new MigrationBuilder("Npgsql.EntityFrameworkCore.PostgreSQL");

        migrationBuilder.EnsurePostgresExtensions();

        var operation = Assert.Single(migrationBuilder.Operations.OfType<SqlOperation>());
        Assert.Contains("CREATE EXTENSION IF NOT EXISTS", operation.Sql);
    }
}
