CREATE SCHEMA IF NOT EXISTS search;

CREATE TABLE search.fts (
    document_id     uuid PRIMARY KEY REFERENCES doc.document(id) ON DELETE CASCADE,
    tsv             tsvector NOT NULL
);
CREATE INDEX search_fts_idx ON search.fts USING GIN (tsv);

CREATE TABLE search.kv (
    document_id     uuid NOT NULL REFERENCES doc.document(id) ON DELETE CASCADE,
    field_key       text NOT NULL,
    field_value     text NOT NULL,
    PRIMARY KEY (document_id, field_key)
);
CREATE INDEX search_kv_value_trgm ON search.kv USING GIN (field_value gin_trgm_ops);

CREATE TABLE search.embedding (
    document_id     uuid PRIMARY KEY REFERENCES doc.document(id) ON DELETE CASCADE,
    title_vec       vector(768),
    text_vec        vector(1536)
);