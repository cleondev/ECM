using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECM.IAM.Infrastructure.Persistence.Migrations;

public partial class RemoveDepartmentFromUsers : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        const string createUnitGroups = @"
WITH unit_groups AS (
    SELECT DISTINCT TRIM(department) AS name
    FROM iam.users
    WHERE department IS NOT NULL AND TRIM(department) <> ''
)
INSERT INTO iam.groups (id, name, kind, created_at)
SELECT uuid_generate_v4(), ug.name, 'unit', now()
FROM unit_groups ug
WHERE NOT EXISTS (
    SELECT 1 FROM iam.groups g WHERE g.name = ug.name
);";

        const string backfillMemberships = @"
WITH user_departments AS (
    SELECT id AS user_id, TRIM(department) AS name
    FROM iam.users
    WHERE department IS NOT NULL AND TRIM(department) <> ''
),
resolved_groups AS (
    SELECT ug.user_id, g.id AS group_id
    FROM user_departments ug
    JOIN iam.groups g ON g.name = ug.name
)
INSERT INTO iam.group_members (group_id, user_id, role, valid_from, valid_to)
SELECT group_id, user_id, 'member', now(), NULL
FROM resolved_groups rg
ON CONFLICT (group_id, user_id)
DO UPDATE SET role = EXCLUDED.role, valid_from = EXCLUDED.valid_from, valid_to = NULL;";

        migrationBuilder.Sql(createUnitGroups);
        migrationBuilder.Sql(backfillMemberships);

        migrationBuilder.DropColumn(
            name: "department",
            schema: "iam",
            table: "users");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        throw new NotSupportedException("This migration cannot be reverted.");
    }
}
