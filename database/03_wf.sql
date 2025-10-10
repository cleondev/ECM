CREATE SCHEMA IF NOT EXISTS wf;

CREATE TABLE wf.definition (
    id          uuid PRIMARY KEY DEFAULT uuid_generate_v4(),
    name        text NOT NULL,
    spec        jsonb NOT NULL,
    is_active   boolean NOT NULL DEFAULT true,
    created_at  timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE wf.instance (
    id              uuid PRIMARY KEY DEFAULT uuid_generate_v4(),
    definition_id   uuid NOT NULL REFERENCES wf.definition(id),
    document_id     uuid REFERENCES doc.document(id) ON DELETE SET NULL,
    state           text NOT NULL,
    created_by      uuid NOT NULL REFERENCES iam.users(id),
    created_at      timestamptz NOT NULL DEFAULT now(),
    updated_at      timestamptz NOT NULL DEFAULT now()
);
CREATE INDEX wf_instance_def_idx ON wf.instance (definition_id);
CREATE INDEX wf_instance_doc_idx ON wf.instance (document_id);

CREATE TABLE wf.task (
    id              uuid PRIMARY KEY DEFAULT uuid_generate_v4(),
    instance_id     uuid NOT NULL REFERENCES wf.instance(id) ON DELETE CASCADE,
    step_key        text NOT NULL,
    assignee_id     uuid,
    due_at          timestamptz,
    status          text NOT NULL DEFAULT 'open',
    acted_at        timestamptz
);
CREATE INDEX wf_task_instance_idx ON wf.task (instance_id);
CREATE INDEX wf_task_assignee_idx ON wf.task (assignee_id, status);

CREATE TABLE wf.form (
    id              uuid PRIMARY KEY DEFAULT uuid_generate_v4(),
    name            text NOT NULL,
    schema_json     jsonb NOT NULL,
    is_active       boolean NOT NULL DEFAULT true,
    created_at      timestamptz NOT NULL DEFAULT now()
);

CREATE TABLE wf.form_data (
    id              uuid PRIMARY KEY DEFAULT uuid_generate_v4(),
    form_id         uuid NOT NULL REFERENCES wf.form(id),
    instance_id     uuid NOT NULL REFERENCES wf.instance(id) ON DELETE CASCADE,
    document_id     uuid REFERENCES doc.document(id) ON DELETE SET NULL,
    data            jsonb NOT NULL,
    created_by      uuid NOT NULL REFERENCES iam.users(id),
    created_at      timestamptz NOT NULL DEFAULT now()
);
CREATE INDEX wf_form_data_form_idx ON wf.form_data (form_id);
CREATE INDEX wf_form_data_instance_idx ON wf.form_data (instance_id);
