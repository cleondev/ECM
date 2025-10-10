CREATE SCHEMA IF NOT EXISTS ocr;

CREATE TABLE ocr.result (
    document_id     uuid NOT NULL REFERENCES doc.document(id) ON DELETE CASCADE,
    version_id      uuid NOT NULL REFERENCES doc.version(id) ON DELETE CASCADE,
    pages           int  NOT NULL,
    lang            text,
    summary         text,
    created_at      timestamptz NOT NULL DEFAULT now(),
    PRIMARY KEY (document_id, version_id)
);

CREATE TABLE ocr.page_text (
    document_id     uuid NOT NULL,
    version_id      uuid NOT NULL,
    page_no         int  NOT NULL,
    content         text NOT NULL,
    PRIMARY KEY (document_id, version_id, page_no),
    FOREIGN KEY (document_id, version_id) REFERENCES ocr.result(document_id, version_id) ON DELETE CASCADE
);
CREATE INDEX ocr_page_text_fts ON ocr.page_text USING GIN (to_tsvector('simple', content));

CREATE TABLE ocr.template (
    id              uuid PRIMARY KEY DEFAULT uuid_generate_v4(),
    name            text NOT NULL,
    version         int  NOT NULL DEFAULT 1,
    page_side       text,
    size_ratio      text,
    is_active       boolean NOT NULL DEFAULT true
);

CREATE TABLE ocr.field_def (
    id              uuid PRIMARY KEY DEFAULT uuid_generate_v4(),
    template_id     uuid NOT NULL REFERENCES ocr.template(id) ON DELETE CASCADE,
    field_key       text NOT NULL,
    bbox_rel        jsonb,
    anchor          jsonb,
    validator       text,
    required        boolean NOT NULL DEFAULT false,
    order_no        int
);

CREATE TABLE ocr.annotation (
    id              uuid PRIMARY KEY DEFAULT uuid_generate_v4(),
    document_id     uuid NOT NULL REFERENCES doc.document(id) ON DELETE CASCADE,
    version_id      uuid NOT NULL REFERENCES doc.version(id)  ON DELETE CASCADE,
    template_id     uuid REFERENCES ocr.template(id),
    field_key       text,
    value_text      text,
    bbox_abs        jsonb,
    confidence      numeric,
    source          text,
    created_by      uuid NOT NULL REFERENCES iam.users(id),
    created_at      timestamptz NOT NULL DEFAULT now()
);
CREATE INDEX ocr_annotation_doc_ver_idx ON ocr.annotation (document_id, version_id);

CREATE TABLE ocr.extraction (
    document_id     uuid NOT NULL REFERENCES doc.document(id) ON DELETE CASCADE,
    version_id      uuid NOT NULL REFERENCES doc.version(id)  ON DELETE CASCADE,
    field_key       text NOT NULL,
    value_text      text,
    confidence      numeric,
    provenance      jsonb,
    PRIMARY KEY (document_id, version_id, field_key)
);