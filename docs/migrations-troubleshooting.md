# Troubleshooting Entity Framework Core migrations

If the migration runner reports that every context is up to date but the target schemas only contain the `__EFMigrationsHistory` table, the migrations were previously applied and the business tables were dropped manually (for example by running `DROP SCHEMA ... CASCADE`). Because EF Core relies on the history table to track applied migrations, it will not recreate the tables until the history entries are removed.

## Verify the migration history

```sql
SELECT migration_id, product_version
FROM iam.__efmigrationshistory
UNION ALL
SELECT migration_id, product_version
FROM doc.__efmigrationshistory
UNION ALL
SELECT migration_id, product_version
FROM file.__efmigrationshistory;
```

If the query returns rows but the schemas are otherwise empty, remove the history entries for the affected module and rerun the migration runner:

```sql
TRUNCATE iam.__efmigrationshistory;
TRUNCATE doc.__efmigrationshistory;
TRUNCATE file.__efmigrationshistory;
```

After truncating the history tables, execute `deploy/scripts/run-migrations.ps1` again. EF Core will recreate the missing tables because it no longer finds the applied migration records.
