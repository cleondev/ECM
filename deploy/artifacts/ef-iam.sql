DO $IAM$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_namespace WHERE nspname = 'iam') THEN
        CREATE SCHEMA iam;
    END IF;
END $IAM$;

BEGIN;

ALTER TABLE iam.users
    ADD COLUMN IF NOT EXISTS primary_group_id uuid;

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
);

WITH user_departments AS (
    SELECT id AS user_id, TRIM(department) AS name
    FROM iam.users
    WHERE department IS NOT NULL AND TRIM(department) <> ''
),
resolved_groups AS (
    SELECT ud.user_id, g.id AS group_id
    FROM user_departments ud
    JOIN iam.groups g ON g.name = ud.name
)
INSERT INTO iam.group_members (group_id, user_id, role, valid_from, valid_to)
SELECT rg.group_id, rg.user_id, 'member', now(), NULL
FROM resolved_groups rg
ON CONFLICT (group_id, user_id)
DO UPDATE SET role = EXCLUDED.role, valid_from = EXCLUDED.valid_from, valid_to = NULL;

WITH user_departments AS (
    SELECT id AS user_id, TRIM(department) AS name
    FROM iam.users
    WHERE department IS NOT NULL AND TRIM(department) <> ''
),
resolved_groups AS (
    SELECT ud.user_id, g.id AS group_id
    FROM user_departments ud
    JOIN iam.groups g ON g.name = ud.name
)
UPDATE iam.users u
SET primary_group_id = COALESCE(u.primary_group_id, rg.group_id)
FROM resolved_groups rg
WHERE u.id = rg.user_id
  AND (u.primary_group_id IS NULL OR u.primary_group_id = rg.group_id);

ALTER TABLE iam.users
    DROP COLUMN IF EXISTS department;

ALTER TABLE iam.groups
    ALTER COLUMN kind SET DEFAULT 'temporary';

CREATE UNIQUE INDEX IF NOT EXISTS ix_users_email ON iam.users (email);
CREATE UNIQUE INDEX IF NOT EXISTS ix_roles_name ON iam.roles (name);
CREATE UNIQUE INDEX IF NOT EXISTS ix_groups_name ON iam.groups (name);

COMMIT;
