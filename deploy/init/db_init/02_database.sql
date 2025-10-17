-- 02_database.sql
-- Tạo database ECM và phân quyền kết nối cơ bản (chạy ở cấp server, Auto-commit ON)

CREATE DATABASE ecm
  WITH OWNER = ecm_owner
  ENCODING 'UTF8'
  LC_COLLATE 'en_US.utf8'
  LC_CTYPE   'en_US.utf8'
  TEMPLATE template0;

REVOKE CONNECT, CREATE, TEMPORARY ON DATABASE ecm FROM PUBLIC;
GRANT CONNECT   ON DATABASE ecm TO ecm_owner, ecm_migrator, ecm_app;
GRANT TEMPORARY ON DATABASE ecm TO ecm_migrator, ecm_app;
