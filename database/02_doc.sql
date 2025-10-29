CREATE SCHEMA IF NOT EXISTS doc;

CREATE TABLE doc.document_type (
    id          uuid PRIMARY KEY DEFAULT uuid_generate_v4(),
    type_key    text UNIQUE NOT NULL,
    type_name   text NOT NULL,
    is_active   boolean NOT NULL DEFAULT true,
    created_at  timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE doc.document (
    id              uuid PRIMARY KEY DEFAULT uuid_generate_v4(),
    title           text NOT NULL,
    doc_type        text NOT NULL,
    status          text NOT NULL,
    sensitivity     text NOT NULL DEFAULT 'Internal',
    owner_id        uuid NOT NULL REFERENCES iam.users(id),
    group_id        uuid,
    created_by      uuid NOT NULL REFERENCES iam.users(id),
    created_at      timestamptz NOT NULL DEFAULT now(),
    updated_at      timestamptz NOT NULL DEFAULT now(),
    type_id         uuid REFERENCES doc.document_type(id)
);
CREATE INDEX doc_document_type_idx ON doc.document (doc_type);
CREATE INDEX doc_document_status_idx ON doc.document (status);
CREATE INDEX doc_document_owner_idx ON doc.document (owner_id);
CREATE INDEX doc_document_updated_at_id_idx ON doc.document (updated_at DESC, id DESC);
CREATE INDEX doc_document_title_fts ON doc.document USING GIN (to_tsvector('simple', coalesce(title, '')));

CREATE TABLE doc.version (
    id              uuid PRIMARY KEY DEFAULT uuid_generate_v4(),
    document_id     uuid NOT NULL REFERENCES doc.document(id) ON DELETE CASCADE,
    version_no      int  NOT NULL,
    storage_key     text NOT NULL,
    bytes           bigint NOT NULL,
    mime_type       text NOT NULL,
    sha256          text NOT NULL,
    created_by      uuid NOT NULL REFERENCES iam.users(id),
    created_at      timestamptz NOT NULL DEFAULT now(),
    UNIQUE (document_id, version_no)
);

CREATE TABLE doc.metadata (
    document_id     uuid PRIMARY KEY REFERENCES doc.document(id) ON DELETE CASCADE,
    data            jsonb NOT NULL DEFAULT '{}'::jsonb
);
CREATE INDEX doc_metadata_gin ON doc.metadata USING GIN (data jsonb_path_ops);

CREATE TABLE doc.file_object (
    storage_key     text PRIMARY KEY,
    legal_hold      boolean NOT NULL DEFAULT false,
    created_at      timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE doc.tag_namespace (
    namespace_slug  text PRIMARY KEY,
    kind            text NOT NULL CHECK (kind IN ('system','user')),
    owner_user_id   uuid REFERENCES iam.users(id),
    display_name    text,
    created_at      timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE doc.tag_label (
    id              uuid PRIMARY KEY DEFAULT uuid_generate_v4(),
    namespace_slug  text NOT NULL REFERENCES doc.tag_namespace(namespace_slug) ON DELETE CASCADE,
    slug            text NOT NULL,
    path            text NOT NULL,
    is_active       boolean NOT NULL DEFAULT true,
    created_by      uuid REFERENCES iam.users(id),
    created_at      timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT uq_tag_path UNIQUE (namespace_slug, path),
    CONSTRAINT chk_tag_path_format CHECK (path ~ '^[a-z0-9_]+(-[a-z0-9_]+)*$')
);
CREATE INDEX tag_label_ns_path_idx ON doc.tag_label (namespace_slug, path);
CREATE INDEX tag_label_path_trgm ON doc.tag_label USING GIN (path gin_trgm_ops);

CREATE TABLE doc.document_tag (
    document_id     uuid NOT NULL REFERENCES doc.document(id) ON DELETE CASCADE,
    tag_id          uuid NOT NULL REFERENCES doc.tag_label(id) ON DELETE CASCADE,
    applied_by      uuid REFERENCES iam.users(id),
    applied_at      timestamptz NOT NULL DEFAULT now(),
    PRIMARY KEY (document_id, tag_id)
);

CREATE TABLE doc.signature_request (
    id              uuid PRIMARY KEY DEFAULT uuid_generate_v4(),
    document_id     uuid NOT NULL REFERENCES doc.document(id) ON DELETE CASCADE,
    version_id      uuid NOT NULL REFERENCES doc.version(id) ON DELETE CASCADE,
    provider        text NOT NULL,
    request_ref     text NOT NULL,
    requested_by    uuid NOT NULL REFERENCES iam.users(id),
    status          text NOT NULL DEFAULT 'pending',
    payload         jsonb NOT NULL DEFAULT '{}'::jsonb,
    created_at      timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE doc.signature_result (
    request_id      uuid PRIMARY KEY REFERENCES doc.signature_request(id) ON DELETE CASCADE,
    status          text NOT NULL,
    evidence_hash   text,
    evidence_url    text,
    received_at     timestamptz NOT NULL DEFAULT now(),
    raw_response    jsonb NOT NULL DEFAULT '{}'::jsonb
);

CREATE TABLE doc.effective_acl_flat (
    document_id     uuid NOT NULL REFERENCES doc.document(id) ON DELETE CASCADE,
    user_id         uuid NOT NULL REFERENCES iam.users(id) ON DELETE CASCADE,
    valid_to        timestamptz,
    source          text NOT NULL,
    idempotency_key text NOT NULL,
    updated_at      timestamptz NOT NULL DEFAULT now(),
    PRIMARY KEY (document_id, user_id, idempotency_key)
);

CREATE INDEX doc_effective_acl_flat_user_document_idx
    ON doc.effective_acl_flat (user_id, valid_to, document_id);