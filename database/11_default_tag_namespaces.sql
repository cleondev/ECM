-- Seed default tag namespaces for the ECM system.
--
-- This script is idempotent. It ensures the existence of:
--   * A global "System" namespace that is marked as system-managed.
--   * A group namespace called "Teams" that is owned by the built-in guest group.
--
-- The guest group is created in database/01_iam.sql with the deterministic id
-- 11111111-1111-1111-1111-111111111111. We still look it up dynamically to
-- avoid hard-coded dependencies in environments that may override seed data.
DO $$
DECLARE
    v_guest_group_id uuid;
BEGIN
    SELECT id
      INTO v_guest_group_id
      FROM iam.groups
     WHERE name = 'guest'
     ORDER BY created_at
     LIMIT 1;

    IF v_guest_group_id IS NULL THEN
        RAISE EXCEPTION 'Không tìm thấy nhóm guest mặc định. Không thể tạo namespace Teams.';
    END IF;

    INSERT INTO doc.tag_namespace (id, scope, display_name, is_system, created_at)
    VALUES (
        'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
        'global',
        'System',
        true,
        now()
    )
    ON CONFLICT (id) DO UPDATE
    SET
        scope = EXCLUDED.scope,
        display_name = EXCLUDED.display_name,
        is_system = EXCLUDED.is_system;

    INSERT INTO doc.tag_namespace (id, scope, owner_group_id, display_name, is_system, created_at)
    VALUES (
        'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb',
        'group',
        v_guest_group_id,
        'Teams',
        false,
        now()
    )
    ON CONFLICT (id) DO UPDATE
    SET
        scope = EXCLUDED.scope,
        owner_group_id = EXCLUDED.owner_group_id,
        display_name = EXCLUDED.display_name,
        is_system = EXCLUDED.is_system;
END $$;
