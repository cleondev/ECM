-- Sample data for ECM document and tagging features.
--
-- The values below are deterministic so the script can be executed multiple
-- times without creating duplicate rows. Each INSERT uses ON CONFLICT to keep
-- the data idempotent while ensuring core attributes stay up to date.

-- Demo team used as the owner of demo documents and tags.
INSERT INTO iam.groups (id, name, kind, created_at)
VALUES (
    '44444444-4444-4444-4444-444444444444',
    'Demo Team',
    'team',
    now()
)
ON CONFLICT (id) DO UPDATE
SET
    name = EXCLUDED.name,
    kind = EXCLUDED.kind;

-- Demo user assigned to the demo team.
INSERT INTO iam.users (id, email, display_name, primary_group_id, is_active, created_at)
VALUES (
    '33333333-3333-3333-3333-333333333333',
    'demo.user@ecm.test',
    'Demo User',
    '44444444-4444-4444-4444-444444444444',
    true,
    now()
)
ON CONFLICT (id) DO UPDATE
SET
    email = EXCLUDED.email,
    display_name = EXCLUDED.display_name,
    primary_group_id = EXCLUDED.primary_group_id,
    is_active = EXCLUDED.is_active;

-- Ensure the demo user is the owner of the demo team.
INSERT INTO iam.group_members (group_id, user_id, role, valid_from)
VALUES (
    '44444444-4444-4444-4444-444444444444',
    '33333333-3333-3333-3333-333333333333',
    'owner',
    now()
)
ON CONFLICT (group_id, user_id) DO UPDATE
SET
    role = EXCLUDED.role,
    valid_to = NULL;

-- Demo document type referenced by the sample document.
INSERT INTO doc.document_type (id, type_key, type_name, is_active, created_at)
VALUES (
    '55555555-5555-5555-5555-555555555555',
    'contract',
    'Contract',
    true,
    now()
)
ON CONFLICT (id) DO UPDATE
SET
    type_key = EXCLUDED.type_key,
    type_name = EXCLUDED.type_name,
    is_active = EXCLUDED.is_active;

-- Tag namespace scoped to the demo team.
INSERT INTO doc.tag_namespace (id, scope, owner_group_id, display_name, is_system, created_at)
VALUES (
    '66666666-6666-6666-6666-666666666666',
    'group',
    '44444444-4444-4444-4444-444444444444',
    'Demo Team Tags',
    false,
    now()
)
ON CONFLICT (id) DO UPDATE
SET
    scope = EXCLUDED.scope,
    owner_group_id = EXCLUDED.owner_group_id,
    display_name = EXCLUDED.display_name,
    is_system = EXCLUDED.is_system;

-- Root tag for the namespace.
INSERT INTO doc.tag_label (
    id,
    namespace_id,
    parent_id,
    name,
    path_ids,
    sort_order,
    color,
    icon_key,
    is_active,
    is_system,
    created_by,
    created_at
)
VALUES (
    '77777777-7777-7777-7777-777777777777',
    '66666666-6666-6666-6666-666666666666',
    NULL,
    'Finance',
    ARRAY['77777777-7777-7777-7777-777777777777'::uuid],
    10,
    '#1B998B',
    'piggy-bank',
    true,
    false,
    '33333333-3333-3333-3333-333333333333',
    now()
)
ON CONFLICT (id) DO UPDATE
SET
    namespace_id = EXCLUDED.namespace_id,
    parent_id = EXCLUDED.parent_id,
    name = EXCLUDED.name,
    path_ids = EXCLUDED.path_ids,
    sort_order = EXCLUDED.sort_order,
    color = EXCLUDED.color,
    icon_key = EXCLUDED.icon_key,
    is_active = EXCLUDED.is_active,
    is_system = EXCLUDED.is_system;

-- Child tag nested under Finance.
INSERT INTO doc.tag_label (
    id,
    namespace_id,
    parent_id,
    name,
    path_ids,
    sort_order,
    color,
    icon_key,
    is_active,
    is_system,
    created_by,
    created_at
)
VALUES (
    '88888888-8888-8888-8888-888888888888',
    '66666666-6666-6666-6666-666666666666',
    '77777777-7777-7777-7777-777777777777',
    'Contracts',
    ARRAY[
        '77777777-7777-7777-7777-777777777777'::uuid,
        '88888888-8888-8888-8888-888888888888'::uuid
    ],
    20,
    '#E84855',
    'file-signature',
    true,
    false,
    '33333333-3333-3333-3333-333333333333',
    now()
)
ON CONFLICT (id) DO UPDATE
SET
    namespace_id = EXCLUDED.namespace_id,
    parent_id = EXCLUDED.parent_id,
    name = EXCLUDED.name,
    path_ids = EXCLUDED.path_ids,
    sort_order = EXCLUDED.sort_order,
    color = EXCLUDED.color,
    icon_key = EXCLUDED.icon_key,
    is_active = EXCLUDED.is_active,
    is_system = EXCLUDED.is_system;

-- Demo document that references the tag hierarchy.
INSERT INTO doc.document (
    id,
    title,
    doc_type,
    status,
    sensitivity,
    owner_id,
    group_id,
    created_by,
    created_at,
    updated_at,
    type_id
)
VALUES (
    '99999999-9999-9999-9999-999999999999',
    'Q1 Partnership Agreement',
    'contract',
    'Published',
    'Confidential',
    '33333333-3333-3333-3333-333333333333',
    '44444444-4444-4444-4444-444444444444',
    '33333333-3333-3333-3333-333333333333',
    now(),
    now(),
    '55555555-5555-5555-5555-555555555555'
)
ON CONFLICT (id) DO UPDATE
SET
    title = EXCLUDED.title,
    doc_type = EXCLUDED.doc_type,
    status = EXCLUDED.status,
    sensitivity = EXCLUDED.sensitivity,
    owner_id = EXCLUDED.owner_id,
    group_id = EXCLUDED.group_id,
    type_id = EXCLUDED.type_id,
    updated_at = now();

-- Apply the Finance and Contracts tags to the demo document.
INSERT INTO doc.document_tag (document_id, tag_id, applied_by, applied_at)
VALUES (
    '99999999-9999-9999-9999-999999999999',
    '77777777-7777-7777-7777-777777777777',
    '33333333-3333-3333-3333-333333333333',
    now()
)
ON CONFLICT (document_id, tag_id) DO UPDATE
SET
    applied_by = EXCLUDED.applied_by,
    applied_at = EXCLUDED.applied_at;

INSERT INTO doc.document_tag (document_id, tag_id, applied_by, applied_at)
VALUES (
    '99999999-9999-9999-9999-999999999999',
    '88888888-8888-8888-8888-888888888888',
    '33333333-3333-3333-3333-333333333333',
    now()
)
ON CONFLICT (document_id, tag_id) DO UPDATE
SET
    applied_by = EXCLUDED.applied_by,
    applied_at = EXCLUDED.applied_at;
