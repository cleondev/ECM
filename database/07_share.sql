DO $$ BEGIN
  IF NOT EXISTS (SELECT 1 FROM pg_type WHERE typname = 'share_subject') THEN
    CREATE TYPE share_subject AS ENUM ('user','group','public');
  END IF;
  IF NOT EXISTS (SELECT 1 FROM pg_type WHERE typname = 'share_perm') THEN
    CREATE TYPE share_perm AS ENUM ('view','download');
  END IF;
END $$;

CREATE TABLE IF NOT EXISTS file.share_link (
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

CREATE INDEX IF NOT EXISTS ix_share_link_doc ON file.share_link (document_id, version_id);
CREATE INDEX IF NOT EXISTS ix_share_link_valid_to ON file.share_link (valid_to);
CREATE INDEX IF NOT EXISTS ix_share_link_owner ON file.share_link (owner_user_id);

CREATE TABLE IF NOT EXISTS file.share_access_event (
  id            bigserial PRIMARY KEY,
  share_id      uuid NOT NULL REFERENCES file.share_link(id) ON DELETE CASCADE,
  occurred_at   timestamptz NOT NULL DEFAULT now(),
  action        text NOT NULL CHECK (action IN ('view','download','password_failed')),
  remote_ip     inet NULL,
  user_agent    text NULL,
  ok            boolean NOT NULL
);

DROP MATERIALIZED VIEW IF EXISTS file.share_stats;
CREATE MATERIALIZED VIEW file.share_stats AS
SELECT share_id,
       count(*) FILTER (WHERE ok AND action='view')      AS views,
       count(*) FILTER (WHERE ok AND action='download')  AS downloads,
       count(*) FILTER (WHERE NOT ok)                    AS failures,
       max(occurred_at)                                  AS last_access
FROM file.share_access_event
GROUP BY share_id;
