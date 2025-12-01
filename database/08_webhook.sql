CREATE SCHEMA IF NOT EXISTS webhook;

CREATE TABLE webhook.delivery (
    id              uuid PRIMARY KEY DEFAULT uuid_generate_v4(),
    request_id      text NOT NULL,
    endpoint_key    text NOT NULL,
    payload         jsonb NOT NULL,
    correlation_id  text,
    status          text NOT NULL DEFAULT 'Pending',
    attempt_count   integer NOT NULL DEFAULT 0,
    last_attempt_at timestamptz,
    last_error      text,
    created_at      timestamptz NOT NULL DEFAULT now(),
    UNIQUE (request_id, endpoint_key)
);
CREATE INDEX webhook_delivery_status_idx ON webhook.delivery (endpoint_key, status);
CREATE INDEX webhook_delivery_created_idx ON webhook.delivery (created_at);
