CREATE SCHEMA IF NOT EXISTS iam;

CREATE TABLE iam.users (
    id              uuid PRIMARY KEY DEFAULT uuid_generate_v4(),
    email           citext UNIQUE NOT NULL,
    display_name    text NOT NULL,
    password_hash   text,
    department      text,
    is_active       boolean NOT NULL DEFAULT true,
    created_at      timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE iam.roles (
    id      uuid PRIMARY KEY DEFAULT uuid_generate_v4(),
    name    text UNIQUE NOT NULL
);

CREATE TABLE iam.user_roles (
    user_id     uuid NOT NULL REFERENCES iam.users(id) ON DELETE CASCADE,
    role_id     uuid NOT NULL REFERENCES iam.roles(id)  ON DELETE CASCADE,
    PRIMARY KEY (user_id, role_id)
);

CREATE TABLE iam.relations (
    subject_id      uuid NOT NULL,
    object_type     text NOT NULL,
    object_id       uuid NOT NULL,
    relation        text NOT NULL,
    created_at      timestamptz NOT NULL DEFAULT now(),
    PRIMARY KEY (subject_id, object_type, object_id, relation)
);
CREATE INDEX iam_relations_object_idx ON iam.relations (object_type, object_id);