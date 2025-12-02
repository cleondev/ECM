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
    id               uuid PRIMARY KEY DEFAULT uuid_generate_v4(),
    scope            text NOT NULL CHECK (scope IN ('global','group','user')),
    owner_user_id    uuid REFERENCES iam.users(id),
    owner_group_id   uuid REFERENCES iam.groups(id),
    display_name     text,
    is_system        boolean NOT NULL DEFAULT false,
    created_at       timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE doc.tag_label (
    id              uuid PRIMARY KEY DEFAULT uuid_generate_v4(),
    namespace_id    uuid NOT NULL REFERENCES doc.tag_namespace(id) ON DELETE CASCADE,
    parent_id       uuid REFERENCES doc.tag_label(id) ON DELETE RESTRICT,
    name            text NOT NULL,
    path_ids        uuid[] NOT NULL DEFAULT ARRAY[]::uuid[],
    sort_order      int NOT NULL DEFAULT 0,
    color           text,
    icon_key        text,
    is_active       boolean NOT NULL DEFAULT true,
    is_system       boolean NOT NULL DEFAULT false,
    created_by      uuid REFERENCES iam.users(id),
    created_at      timestamptz NOT NULL DEFAULT now(),
    CONSTRAINT uq_tag_sibling_name UNIQUE (namespace_id, parent_id, name)
);
CREATE INDEX tag_label_ns_parent_idx ON doc.tag_label (namespace_id, parent_id);
CREATE INDEX tag_label_ns_path_gin ON doc.tag_label USING GIN (path_ids);
CREATE INDEX tag_label_name_trgm ON doc.tag_label USING GIN (name gin_trgm_ops);

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

DO $$ BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_type WHERE typname = 'share_subject') THEN
    CREATE TYPE share_subject AS ENUM ('user','group','public');
  END IF;
  IF NOT EXISTS (SELECT 1 FROM pg_type WHERE typname = 'share_perm') THEN
    CREATE TYPE share_perm AS ENUM ('view','download');
  END IF;
END $$;

CREATE TABLE IF NOT EXISTS doc.share_link (
  id              uuid PRIMARY KEY DEFAULT gen_random_uuid(),
  code            varchar(16) UNIQUE NOT NULL,
  owner_user_id   uuid NOT NULL,
  document_id     uuid NOT NULL,
  version_id      uuid NULL,
  subject_type    share_subject NOT NULL,
  subject_id      uuid NULL,
  permissions     share_perm[] NOT NULL DEFAULT '{view,download}',
  password_hash   text NULL,
  valid_from      timestamptz NOT NULL DEFAULT now(),
  valid_to        timestamptz NULL,
  max_views       int NULL,
  max_downloads   int NULL,
  file_name       varchar(512) NOT NULL,
  file_extension  varchar(32) NULL,
  file_content_type varchar(256) NOT NULL,
  file_size_bytes bigint NOT NULL CHECK (file_size_bytes >= 0),
  file_created_at timestamptz NULL,
  watermark       jsonb NULL,
  allowed_ips     cidr[] NULL,
  created_at      timestamptz NOT NULL DEFAULT now(),
  revoked_at      timestamptz NULL,
  CONSTRAINT chk_share_link_valid_window CHECK (valid_to IS NULL OR valid_to >= valid_from),
  CONSTRAINT chk_share_link_subject CHECK (
    (subject_type = 'public' AND subject_id IS NULL)
    OR (subject_type IN ('user','group') AND subject_id IS NOT NULL)
  )
);

CREATE INDEX IF NOT EXISTS ix_share_link_doc ON doc.share_link (document_id, version_id);
CREATE INDEX IF NOT EXISTS ix_share_link_valid_to ON doc.share_link (valid_to);
CREATE INDEX IF NOT EXISTS ix_share_link_owner ON doc.share_link (owner_user_id);

CREATE TABLE IF NOT EXISTS doc.share_access_event (
  id            bigserial PRIMARY KEY,
  share_id      uuid NOT NULL REFERENCES doc.share_link(id) ON DELETE CASCADE,
  occurred_at   timestamptz NOT NULL DEFAULT now(),
  action        text NOT NULL CHECK (action IN ('view','download','password_failed')),
  remote_ip     inet NULL,
  user_agent    text NULL,
  ok            boolean NOT NULL
);

DROP MATERIALIZED VIEW IF EXISTS doc.share_stats;
CREATE MATERIALIZED VIEW doc.share_stats AS
SELECT share_id,
       count(*) FILTER (WHERE ok AND action='view')      AS views,
       count(*) FILTER (WHERE ok AND action='download')  AS downloads,
       count(*) FILTER (WHERE NOT ok)                    AS failures,
       max(occurred_at)                                  AS last_access
FROM doc.share_access_event
GROUP BY share_id;
