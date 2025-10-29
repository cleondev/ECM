CREATE SCHEMA IF NOT EXISTS iam;

CREATE TABLE iam.users (
    id               uuid PRIMARY KEY DEFAULT uuid_generate_v4(),
    email            citext NOT NULL,
    display_name     text NOT NULL,
    primary_group_id uuid,
    password_hash    text,
    is_active        boolean NOT NULL DEFAULT true,
    created_at       timestamptz NOT NULL DEFAULT now()
);

CREATE UNIQUE INDEX ix_users_email ON iam.users (email);

CREATE TABLE iam.roles (
    id      uuid PRIMARY KEY DEFAULT uuid_generate_v4(),
    name    text NOT NULL
);

CREATE UNIQUE INDEX ix_roles_name ON iam.roles (name);

CREATE TABLE iam.user_roles (
    user_id     uuid NOT NULL REFERENCES iam.users(id) ON DELETE CASCADE,
    role_id     uuid NOT NULL REFERENCES iam.roles(id)  ON DELETE CASCADE,
    PRIMARY KEY (user_id, role_id)
);

CREATE TABLE iam.groups (
    id              uuid PRIMARY KEY DEFAULT uuid_generate_v4(),
    name            text NOT NULL,
    kind            text NOT NULL DEFAULT 'temporary',
    parent_group_id uuid REFERENCES iam.groups(id) ON DELETE RESTRICT,
    created_by      uuid REFERENCES iam.users(id),
    created_at      timestamptz NOT NULL DEFAULT now()
);

CREATE UNIQUE INDEX ix_groups_name
    ON iam.groups (name);

INSERT INTO iam.groups (id, name, kind, created_at)
VALUES
    ('11111111-1111-1111-1111-111111111111', 'guest', 'system', now()),
    ('22222222-2222-2222-2222-222222222222', 'system', 'system', now())
ON CONFLICT (name) DO NOTHING;

CREATE INDEX ix_groups_parent_group_id
    ON iam.groups (parent_group_id);

CREATE TABLE iam.group_members (
    group_id    uuid NOT NULL REFERENCES iam.groups(id) ON DELETE CASCADE,
    user_id     uuid NOT NULL REFERENCES iam.users(id) ON DELETE CASCADE,
    role        text NOT NULL DEFAULT 'member',
    valid_from  timestamptz NOT NULL DEFAULT now(),
    valid_to    timestamptz,
    PRIMARY KEY (group_id, user_id)
);

CREATE TABLE iam.relations (
    subject_id      uuid NOT NULL,
    subject_type    text NOT NULL DEFAULT 'user' CHECK (subject_type IN ('user','group')),
    object_type     text NOT NULL,
    object_id       uuid NOT NULL,
    relation        text NOT NULL,
    valid_from      timestamptz NOT NULL DEFAULT now(),
    valid_to        timestamptz,
    created_at      timestamptz NOT NULL DEFAULT now(),
    PRIMARY KEY (subject_type, subject_id, object_type, object_id, relation)
);

CREATE INDEX iam_relations_object_idx
    ON iam.relations (object_type, object_id);

CREATE INDEX iam_relations_object_subject_idx
    ON iam.relations (object_type, relation, subject_type, subject_id, object_id);

CREATE INDEX iam_group_members_active_user_idx
    ON iam.group_members (user_id)
    WHERE valid_to IS NULL OR valid_to >= now();

CREATE INDEX iam_group_members_group_validity_idx
    ON iam.group_members (group_id, valid_from, valid_to);
