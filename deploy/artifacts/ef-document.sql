DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM pg_namespace WHERE nspname = 'doc') THEN
        CREATE SCHEMA doc;
    END IF;
END $EF$;
CREATE TABLE IF NOT EXISTS doc."__EFMigrationsHistory" (
    migration_id character varying(150) NOT NULL,
    product_version character varying(32) NOT NULL,
    CONSTRAINT pk___ef_migrations_history PRIMARY KEY (migration_id)
);

START TRANSACTION;
DO $EF$
BEGIN
    IF NOT EXISTS(SELECT 1 FROM pg_namespace WHERE nspname = 'doc') THEN
        CREATE SCHEMA doc;
    END IF;
END $EF$;

CREATE TABLE doc.document_type (
    id uuid NOT NULL,
    type_key text NOT NULL,
    type_name text NOT NULL,
    is_active boolean NOT NULL DEFAULT TRUE,
    created_at timestamptz NOT NULL DEFAULT (now()),
    CONSTRAINT pk_document_type PRIMARY KEY (id)
);

CREATE TABLE doc.file_object (
    storage_key text NOT NULL,
    legal_hold boolean NOT NULL DEFAULT FALSE,
    created_at timestamptz NOT NULL DEFAULT (now()),
    CONSTRAINT pk_file_object PRIMARY KEY (storage_key)
);

CREATE TABLE doc.tag_namespace (
    namespace_slug text NOT NULL,
    kind text NOT NULL,
    owner_user_id uuid,
    display_name text,
    created_at timestamptz NOT NULL DEFAULT (now()),
    CONSTRAINT pk_tag_namespace PRIMARY KEY (namespace_slug),
    CONSTRAINT chk_tag_namespace_kind CHECK (kind IN ('system','user'))
);

CREATE TABLE doc.document (
    id uuid NOT NULL,
    title text NOT NULL,
    doc_type text NOT NULL,
    status text NOT NULL,
    sensitivity text NOT NULL DEFAULT 'Internal',
    owner_id uuid NOT NULL,
    group_id uuid,
    created_by uuid NOT NULL,
    created_at timestamptz NOT NULL DEFAULT (now()),
    updated_at timestamptz NOT NULL DEFAULT (now()),
    type_id uuid,
    CONSTRAINT pk_document PRIMARY KEY (id),
    CONSTRAINT fk_document_document_types_type_id FOREIGN KEY (type_id) REFERENCES doc.document_type (id) ON DELETE RESTRICT
);

CREATE TABLE doc.tag_label (
    id uuid NOT NULL,
    namespace_slug text NOT NULL,
    slug text NOT NULL,
    path text NOT NULL,
    is_active boolean NOT NULL DEFAULT TRUE,
    created_by uuid,
    created_at timestamptz NOT NULL DEFAULT (now()),
    CONSTRAINT pk_tag_label PRIMARY KEY (id),
    CONSTRAINT chk_tag_path_format CHECK (path ~ '^[a-z0-9_]+(-[a-z0-9_]+)*$'),
    CONSTRAINT fk_tag_label_tag_namespace_namespace_slug FOREIGN KEY (namespace_slug) REFERENCES doc.tag_namespace (namespace_slug) ON DELETE CASCADE
);

CREATE TABLE doc.metadata (
    document_id uuid NOT NULL,
    data jsonb NOT NULL DEFAULT ('{}'::jsonb),
    CONSTRAINT pk_metadata PRIMARY KEY (document_id),
    CONSTRAINT fk_metadata_document_document_id FOREIGN KEY (document_id) REFERENCES doc.document (id) ON DELETE CASCADE
);

CREATE TABLE doc.version (
    id uuid NOT NULL,
    document_id uuid NOT NULL,
    version_no integer NOT NULL,
    storage_key text NOT NULL,
    bytes bigint NOT NULL,
    mime_type text NOT NULL,
    sha256 text NOT NULL,
    created_by uuid NOT NULL,
    created_at timestamptz NOT NULL DEFAULT (now()),
    CONSTRAINT pk_version PRIMARY KEY (id),
    CONSTRAINT fk_version_document_document_id FOREIGN KEY (document_id) REFERENCES doc.document (id) ON DELETE CASCADE
);

CREATE TABLE doc.document_tag (
    document_id uuid NOT NULL,
    tag_id uuid NOT NULL,
    applied_by uuid,
    applied_at timestamptz NOT NULL DEFAULT (now()),
    CONSTRAINT pk_document_tag PRIMARY KEY (document_id, tag_id),
    CONSTRAINT fk_document_tag_document_document_id FOREIGN KEY (document_id) REFERENCES doc.document (id) ON DELETE CASCADE,
    CONSTRAINT fk_document_tag_tag_labels_tag_id FOREIGN KEY (tag_id) REFERENCES doc.tag_label (id) ON DELETE CASCADE
);

CREATE TABLE doc.signature_request (
    id uuid NOT NULL,
    document_id uuid NOT NULL,
    version_id uuid NOT NULL,
    provider text NOT NULL,
    request_ref text NOT NULL,
    requested_by uuid NOT NULL,
    status text NOT NULL DEFAULT 'pending',
    payload jsonb NOT NULL DEFAULT ('{}'::jsonb),
    created_at timestamptz NOT NULL DEFAULT (now()),
    CONSTRAINT pk_signature_request PRIMARY KEY (id),
    CONSTRAINT fk_signature_request_document_document_id FOREIGN KEY (document_id) REFERENCES doc.document (id) ON DELETE CASCADE,
    CONSTRAINT fk_signature_request_version_version_id FOREIGN KEY (version_id) REFERENCES doc.version (id) ON DELETE CASCADE
);

CREATE TABLE doc.signature_result (
    request_id uuid NOT NULL,
    status text NOT NULL,
    evidence_hash text,
    evidence_url text,
    received_at timestamptz NOT NULL DEFAULT (now()),
    raw_response jsonb NOT NULL DEFAULT ('{}'::jsonb),
    CONSTRAINT pk_signature_result PRIMARY KEY (request_id),
    CONSTRAINT fk_signature_result_signature_request_request_id FOREIGN KEY (request_id) REFERENCES doc.signature_request (id) ON DELETE CASCADE
);

CREATE INDEX doc_document_owner_idx ON doc.document (owner_id);

CREATE INDEX doc_document_status_idx ON doc.document (status);

CREATE INDEX doc_document_type_idx ON doc.document (doc_type);

CREATE INDEX "IX_document_created_by" ON doc.document (created_by);

CREATE INDEX "IX_document_type_id" ON doc.document (type_id);

CREATE INDEX "IX_document_tag_applied_by" ON doc.document_tag (applied_by);

CREATE INDEX "IX_document_tag_tag_id" ON doc.document_tag (tag_id);

CREATE UNIQUE INDEX "IX_document_type_type_key" ON doc.document_type (type_key);

CREATE INDEX "IX_signature_request_document_id" ON doc.signature_request (document_id);

CREATE INDEX "IX_signature_request_requested_by" ON doc.signature_request (requested_by);

CREATE INDEX "IX_signature_request_version_id" ON doc.signature_request (version_id);

CREATE INDEX "IX_tag_label_created_by" ON doc.tag_label (created_by);

CREATE INDEX "IX_tag_label_namespace_slug" ON doc.tag_label (namespace_slug);

CREATE UNIQUE INDEX tag_label_ns_path_idx ON doc.tag_label (namespace_slug, path);

CREATE INDEX "IX_version_created_by" ON doc.version (created_by);

CREATE INDEX "IX_version_document_id" ON doc.version (document_id);

CREATE UNIQUE INDEX "IX_version_document_id_version_no" ON doc.version (document_id, version_no);

INSERT INTO doc."__EFMigrationsHistory" (migration_id, product_version)
VALUES ('20251021035704_InitDocumentDb', '9.0.10');

COMMIT;

