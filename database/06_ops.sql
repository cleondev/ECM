CREATE SCHEMA IF NOT EXISTS ops;

CREATE TABLE ops.outbox (
    id              bigserial PRIMARY KEY,
    aggregate       text NOT NULL,
    aggregate_id    uuid NOT NULL,
    type            text NOT NULL,
    payload         jsonb NOT NULL,
    occurred_at     timestamptz NOT NULL DEFAULT now(),
    processed_at    timestamptz
);
CREATE INDEX ops_outbox_processed_idx ON ops.outbox (processed_at NULLS FIRST);
CREATE INDEX ops_outbox_agg_idx ON ops.outbox (aggregate, aggregate_id);

CREATE TABLE ops.outbox_deadletter (
    id              bigint PRIMARY KEY,
    type            text NOT NULL,
    payload         jsonb NOT NULL,
    error           text NOT NULL,
    failed_at       timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE ops.audit_event (
    id              bigserial PRIMARY KEY,
    occurred_at     timestamptz NOT NULL DEFAULT now(),
    actor_id        uuid,
    action          text NOT NULL,
    object_type     text NOT NULL,
    object_id       uuid NOT NULL,
    details         jsonb NOT NULL DEFAULT '{}'::jsonb
);
CREATE INDEX ops_audit_obj_idx ON ops.audit_event (object_type, object_id);
CREATE INDEX ops_audit_time_idx ON ops.audit_event (occurred_at);

CREATE TABLE ops.retention_policy (
    id              uuid PRIMARY KEY DEFAULT uuid_generate_v4(),
    name            text NOT NULL,
    rule            jsonb NOT NULL,
    is_active       boolean NOT NULL DEFAULT true,
    created_at      timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE ops.retention_candidate (
    document_id     uuid PRIMARY KEY REFERENCES doc.document(id) ON DELETE CASCADE,
    policy_id       uuid NOT NULL REFERENCES ops.retention_policy(id),
    due_at          timestamptz NOT NULL,
    reason          text
);
CREATE INDEX ops_retention_due_idx ON ops.retention_candidate (due_at);
