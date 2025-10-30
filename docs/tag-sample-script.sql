-- Sample script to seed tag namespaces and label hierarchies for global, team, and user scopes.
--
-- Update the value of v_target_email to match the user you want to base the sample data on.
-- The script will ensure the following:
--   * A global namespace with a simple hierarchy (Company Docs -> Policies).
--   * A team namespace for the user's primary group (Team Projects -> Roadmaps).
--   * A personal namespace for every active member of that team (Personal Workspace -> Drafts).
--
-- Requirements:
--   * Extension "uuid-ossp" must be available for uuid_generate_v4().
--   * The IAM tables (iam.users, iam.groups, iam.group_members) and document tables must exist.
--
-- You can run this block multiple times safely; it updates existing rows to keep data idempotent.
DO $$
DECLARE
    v_target_email      text := 'demo.user@ecm.test'; -- <-- change to your target user's email
    v_target_user_id    uuid;
    v_target_group_id   uuid;
    v_namespace_id      uuid;
    v_root_id           uuid;
    v_child_id          uuid;
    v_member_user_id    uuid;
BEGIN
    -- Locate the target user and their primary team.
    SELECT u.id, u.primary_group_id
      INTO v_target_user_id, v_target_group_id
      FROM iam.users u
     WHERE u.email = v_target_email
       AND u.is_active
     LIMIT 1;

    IF v_target_user_id IS NULL THEN
        RAISE EXCEPTION 'Không tìm thấy người dùng với email % hoặc người dùng không hoạt động.', v_target_email;
    END IF;

    IF v_target_group_id IS NULL THEN
        RAISE EXCEPTION 'Người dùng % không có primary_group_id. Không thể tạo namespace team.', v_target_email;
    END IF;

    ------------------------------------------------------------------
    -- 1. Global namespace shared by everyone.
    ------------------------------------------------------------------
    SELECT id
      INTO v_namespace_id
      FROM doc.tag_namespace
     WHERE scope = 'global'
       AND display_name = 'Global Demo Tags'
     LIMIT 1;

    IF v_namespace_id IS NULL THEN
        v_namespace_id := uuid_generate_v4();
        INSERT INTO doc.tag_namespace (id, scope, display_name, is_system, created_at)
        VALUES (v_namespace_id, 'global', 'Global Demo Tags', false, now());
    ELSE
        UPDATE doc.tag_namespace
           SET display_name = 'Global Demo Tags',
               is_system    = false
         WHERE id = v_namespace_id;
    END IF;

    -- Global root tag.
    SELECT id
      INTO v_root_id
      FROM doc.tag_label
     WHERE namespace_id = v_namespace_id
       AND parent_id IS NULL
       AND name = 'Company Docs';

    IF v_root_id IS NULL THEN
        v_root_id := uuid_generate_v4();
        INSERT INTO doc.tag_label (
            id, namespace_id, parent_id, name, path_ids,
            sort_order, color, icon_key, is_active, is_system,
            created_by, created_at
        ) VALUES (
            v_root_id, v_namespace_id, NULL, 'Company Docs',
            ARRAY[v_root_id], 10, '#0D9488', 'building', true, false,
            v_target_user_id, now()
        );
    ELSE
        UPDATE doc.tag_label
           SET sort_order = 10,
               color      = '#0D9488',
               icon_key   = 'building',
               is_active  = true,
               is_system  = false
         WHERE id = v_root_id;
    END IF;

    -- Global child tag.
    SELECT id
      INTO v_child_id
      FROM doc.tag_label
     WHERE namespace_id = v_namespace_id
       AND parent_id = v_root_id
       AND name = 'Policies';

    IF v_child_id IS NULL THEN
        v_child_id := uuid_generate_v4();
        INSERT INTO doc.tag_label (
            id, namespace_id, parent_id, name, path_ids,
            sort_order, color, icon_key, is_active, is_system,
            created_by, created_at
        ) VALUES (
            v_child_id, v_namespace_id, v_root_id, 'Policies',
            ARRAY[v_root_id, v_child_id], 20, '#F97316', 'scale-balanced', true, false,
            v_target_user_id, now()
        );
    ELSE
        UPDATE doc.tag_label
           SET sort_order = 20,
               color      = '#F97316',
               icon_key   = 'scale-balanced',
               is_active  = true,
               is_system  = false
         WHERE id = v_child_id;
    END IF;

    ------------------------------------------------------------------
    -- 2. Team namespace for the user's primary group.
    ------------------------------------------------------------------
    SELECT id
      INTO v_namespace_id
      FROM doc.tag_namespace
     WHERE scope = 'group'
       AND owner_group_id = v_target_group_id
     LIMIT 1;

    IF v_namespace_id IS NULL THEN
        v_namespace_id := uuid_generate_v4();
        INSERT INTO doc.tag_namespace (
            id, scope, owner_group_id, display_name, is_system, created_at
        ) VALUES (
            v_namespace_id, 'group', v_target_group_id, 'Team Demo Tags', false, now()
        );
    ELSE
        UPDATE doc.tag_namespace
           SET display_name = 'Team Demo Tags',
               is_system    = false
         WHERE id = v_namespace_id;
    END IF;

    -- Team root tag.
    SELECT id
      INTO v_root_id
      FROM doc.tag_label
     WHERE namespace_id = v_namespace_id
       AND parent_id IS NULL
       AND name = 'Team Projects';

    IF v_root_id IS NULL THEN
        v_root_id := uuid_generate_v4();
        INSERT INTO doc.tag_label (
            id, namespace_id, parent_id, name, path_ids,
            sort_order, color, icon_key, is_active, is_system,
            created_by, created_at
        ) VALUES (
            v_root_id, v_namespace_id, NULL, 'Team Projects',
            ARRAY[v_root_id], 10, '#2563EB', 'people-group', true, false,
            v_target_user_id, now()
        );
    ELSE
        UPDATE doc.tag_label
           SET sort_order = 10,
               color      = '#2563EB',
               icon_key   = 'people-group',
               is_active  = true,
               is_system  = false
         WHERE id = v_root_id;
    END IF;

    -- Team child tag.
    SELECT id
      INTO v_child_id
      FROM doc.tag_label
     WHERE namespace_id = v_namespace_id
       AND parent_id = v_root_id
       AND name = 'Roadmaps';

    IF v_child_id IS NULL THEN
        v_child_id := uuid_generate_v4();
        INSERT INTO doc.tag_label (
            id, namespace_id, parent_id, name, path_ids,
            sort_order, color, icon_key, is_active, is_system,
            created_by, created_at
        ) VALUES (
            v_child_id, v_namespace_id, v_root_id, 'Roadmaps',
            ARRAY[v_root_id, v_child_id], 20, '#7C3AED', 'map', true, false,
            v_target_user_id, now()
        );
    ELSE
        UPDATE doc.tag_label
           SET sort_order = 20,
               color      = '#7C3AED',
               icon_key   = 'map',
               is_active  = true,
               is_system  = false
         WHERE id = v_child_id;
    END IF;

    ------------------------------------------------------------------
    -- 3. Personal namespaces for every active member in the team.
    ------------------------------------------------------------------
    FOR v_member_user_id IN
        SELECT DISTINCT gm.user_id
          FROM iam.group_members gm
          JOIN iam.users u ON u.id = gm.user_id
         WHERE gm.group_id = v_target_group_id
           AND gm.valid_to IS NULL
           AND u.is_active
    LOOP
        SELECT id
          INTO v_namespace_id
          FROM doc.tag_namespace
         WHERE scope = 'user'
           AND owner_user_id = v_member_user_id
         LIMIT 1;

        IF v_namespace_id IS NULL THEN
            v_namespace_id := uuid_generate_v4();
            INSERT INTO doc.tag_namespace (
                id, scope, owner_user_id, display_name, is_system, created_at
            ) VALUES (
                v_namespace_id, 'user', v_member_user_id, 'Personal Demo Tags', false, now()
            );
        ELSE
            UPDATE doc.tag_namespace
               SET display_name = 'Personal Demo Tags',
                   is_system    = false
             WHERE id = v_namespace_id;
        END IF;

        -- Personal root tag.
        SELECT id
          INTO v_root_id
          FROM doc.tag_label
         WHERE namespace_id = v_namespace_id
           AND parent_id IS NULL
           AND name = 'Personal Workspace';

        IF v_root_id IS NULL THEN
            v_root_id := uuid_generate_v4();
            INSERT INTO doc.tag_label (
                id, namespace_id, parent_id, name, path_ids,
                sort_order, color, icon_key, is_active, is_system,
                created_by, created_at
            ) VALUES (
                v_root_id, v_namespace_id, NULL, 'Personal Workspace',
                ARRAY[v_root_id], 10, '#059669', 'user', true, false,
                v_member_user_id, now()
            );
        ELSE
            UPDATE doc.tag_label
               SET sort_order = 10,
                   color      = '#059669',
                   icon_key   = 'user',
                   is_active  = true,
                   is_system  = false
             WHERE id = v_root_id;
        END IF;

        -- Personal child tag.
        SELECT id
          INTO v_child_id
          FROM doc.tag_label
         WHERE namespace_id = v_namespace_id
           AND parent_id = v_root_id
           AND name = 'Drafts';

        IF v_child_id IS NULL THEN
            v_child_id := uuid_generate_v4();
            INSERT INTO doc.tag_label (
                id, namespace_id, parent_id, name, path_ids,
                sort_order, color, icon_key, is_active, is_system,
                created_by, created_at
            ) VALUES (
                v_child_id, v_namespace_id, v_root_id, 'Drafts',
                ARRAY[v_root_id, v_child_id], 20, '#D97706', 'file-pen', true, false,
                v_member_user_id, now()
            );
        ELSE
            UPDATE doc.tag_label
               SET sort_order = 20,
                   color      = '#D97706',
                   icon_key   = 'file-pen',
                   is_active  = true,
                   is_system  = false
             WHERE id = v_child_id;
        END IF;
    END LOOP;
END;
$$;
