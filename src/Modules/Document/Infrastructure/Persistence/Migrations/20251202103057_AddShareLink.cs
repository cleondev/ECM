using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECM.Document.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddShareLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
            @"DO $$ BEGIN
              IF NOT EXISTS (SELECT 1 FROM pg_type WHERE typname = 'share_subject') THEN
                CREATE TYPE share_subject AS ENUM ('user','group','public');
              END IF;
              IF NOT EXISTS (SELECT 1 FROM pg_type WHERE typname = 'share_perm') THEN
                CREATE TYPE share_perm AS ENUM ('view','download');
              END IF;
            END $$;

            CREATE INDEX IF NOT EXISTS ix_share_link_doc ON doc.share_link (document_id, version_id);
            CREATE INDEX IF NOT EXISTS ix_share_link_valid_to ON doc.share_link (valid_to);
            CREATE INDEX IF NOT EXISTS ix_share_link_owner ON doc.share_link (owner_user_id);

            CREATE TABLE IF NOT EXISTS doc.share_access_event (
              id            bigserial PRIMARY KEY,
              share_id      uuid NOT NULL REFERENCES file.share_link(id) ON DELETE CASCADE,
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
            GROUP BY share_id;");
    }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
            @"DROP MATERIALIZED VIEW IF EXISTS doc.share_stats;
            DROP TYPE IF EXISTS share_perm;
            DROP TYPE IF EXISTS share_subject;");
        }
    }
}
