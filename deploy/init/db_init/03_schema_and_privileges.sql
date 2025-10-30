-- 03_schema_and_privileges.sql
-- Tạo schema, phân quyền, thiết lập default privileges, và cài extensions
-- CHẠY TRONG DB 'ecm'

-- Khóa public schema
ALTER SCHEMA public OWNER TO ecm_owner;
REVOKE ALL ON SCHEMA public FROM PUBLIC;
GRANT USAGE ON SCHEMA public TO ecm_app, ecm_migrator;

-- Tạo schema cho từng module
CREATE SCHEMA IF NOT EXISTS iam    AUTHORIZATION ecm_owner;
CREATE SCHEMA IF NOT EXISTS doc    AUTHORIZATION ecm_owner;
CREATE SCHEMA IF NOT EXISTS file   AUTHORIZATION ecm_owner;
CREATE SCHEMA IF NOT EXISTS wf     AUTHORIZATION ecm_owner;
CREATE SCHEMA IF NOT EXISTS search AUTHORIZATION ecm_owner;
CREATE SCHEMA IF NOT EXISTS ocr    AUTHORIZATION ecm_owner;
CREATE SCHEMA IF NOT EXISTS ops    AUTHORIZATION ecm_owner;

-- Phân quyền
GRANT USAGE ON SCHEMA iam, doc, file, wf, search, ocr, ops TO ecm_app;
GRANT USAGE, CREATE ON SCHEMA iam, doc, file, wf, search, ocr, ops TO ecm_migrator;

-- Default privileges: cấp quyền cho object tạo mới
ALTER DEFAULT PRIVILEGES FOR ROLE ecm_owner IN SCHEMA iam, doc, file, wf, search, ocr, ops
  GRANT SELECT, INSERT, UPDATE, DELETE, REFERENCES ON TABLES TO ecm_app;
ALTER DEFAULT PRIVILEGES FOR ROLE ecm_owner IN SCHEMA iam, doc, file, wf, search, ocr, ops
  GRANT USAGE, SELECT, UPDATE ON SEQUENCES TO ecm_app;
ALTER DEFAULT PRIVILEGES FOR ROLE ecm_owner IN SCHEMA iam, doc, file, wf, search, ocr, ops
  GRANT EXECUTE ON FUNCTIONS TO ecm_app;
ALTER DEFAULT PRIVILEGES FOR ROLE ecm_owner IN SCHEMA iam, doc, file, wf, search, ocr, ops
  GRANT USAGE ON TYPES TO ecm_app;

ALTER DEFAULT PRIVILEGES FOR ROLE ecm_migrator IN SCHEMA iam, doc, file, wf, search, ocr, ops
  GRANT SELECT, INSERT, UPDATE, DELETE, REFERENCES ON TABLES TO ecm_app;
ALTER DEFAULT PRIVILEGES FOR ROLE ecm_migrator IN SCHEMA iam, doc, file, wf, search, ocr, ops
  GRANT USAGE, SELECT, UPDATE ON SEQUENCES TO ecm_app;
ALTER DEFAULT PRIVILEGES FOR ROLE ecm_migrator IN SCHEMA iam, doc, file, wf, search, ocr, ops
  GRANT EXECUTE ON FUNCTIONS TO ecm_app;
ALTER DEFAULT PRIVILEGES FOR ROLE ecm_migrator IN SCHEMA iam, doc, file, wf, search, ocr, ops
  GRANT USAGE ON TYPES TO ecm_app;

-- Áp quyền cho object hiện có
GRANT SELECT, INSERT, UPDATE, DELETE, REFERENCES ON ALL TABLES    IN SCHEMA iam, doc, file, wf, search, ocr, ops TO ecm_app;
GRANT USAGE, SELECT, UPDATE                  ON ALL SEQUENCES IN SCHEMA iam, doc, file, wf, search, ocr, ops TO ecm_app;
GRANT EXECUTE                                ON ALL FUNCTIONS IN SCHEMA iam, doc, file, wf, search, ocr, ops TO ecm_app;

-- Cài extension hay dùng
CREATE EXTENSION IF NOT EXISTS "uuid-ossp" WITH SCHEMA public;
CREATE EXTENSION IF NOT EXISTS pgcrypto     WITH SCHEMA public;
CREATE EXTENSION IF NOT EXISTS citext       WITH SCHEMA public;
-- CREATE EXTENSION IF NOT EXISTS vector    WITH SCHEMA public;
