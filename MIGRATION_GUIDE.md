# EF Tools – Hướng dẫn nhanh

Tất cả chạy từ **root repo** (nơi có `ecm.ps1`).

## Lệnh cơ bản

```powershell
# Tạo migration (module đơn lẻ)
.\ecm.ps1 ef add iam InitIamDb
.\ecm.ps1 ef add document AddIndexes
.\ecm.ps1 ef add file InitFileDb
.\ecm.ps1 ef add ocr InitOcrDb
.\ecm.ps1 ef add operations InitOperations

# Apply DB (module đơn lẻ hoặc all)
.\ecm.ps1 ef update iam
.\ecm.ps1 ef update ocr
.\ecm.ps1 ef update operations
.\ecm.ps1 ef update all

# Rollback về migration đích ("0" = trống)
.\ecm.ps1 ef rollback document -name 0
.\ecm.ps1 ef rollback ocr -name 0
.\ecm.ps1 ef rollback iam -name 20251017_AddUserIndex
.\ecm.ps1 ef rollback operations -name 0
.\ecm.ps1 ef rollback all -name 0

# Generate SQL (per module hoặc all)
.\ecm.ps1 ef script iam
.\ecm.ps1 ef script document -from 20251010_Init -to 20251017_AddX -idempotent
.\ecm.ps1 ef script file -idempotent
.\ecm.ps1 ef script ocr -idempotent
.\ecm.ps1 ef script operations -idempotent
.\ecm.ps1 ef script all -idempotent

# Liệt kê DbContext và danh sách migrations
.\ecm.ps1 ef list
.\ecm.ps1 ef miglist iam
.\ecm.ps1 ef miglist all
```

## Aliases (nạp cho session hiện tại)

```powershell
.\ecm.ps1 alias

# ADD
ef-iam-add InitIamDb
ef-doc-add AddMetadata
ef-file-add InitFileDb
ef-ocr-add InitOcrDb
ef-operations-add InitOperations

# UPDATE
ef-iam-update
ef-doc-update
ef-file-update
ef-ocr-update
ef-operations-update
ef-update-all

# ROLLBACK
ef-iam-rollback 0
ef-doc-rollback 20251017_AddX
ef-file-rollback 0
ef-ocr-rollback 0
ef-operations-rollback 0
ef-rollback-all 0

# SCRIPT
ef-iam-script
ef-doc-script -From 20251010_Init -To 20251017_AddX -Idempotent
ef-file-script -Idempotent
ef-ocr-script -Idempotent
ef-operations-script -Idempotent
ef-script-all -Idempotent

# LIST
ef-list
ef-miglist-iam
ef-miglist-doc
ef-miglist-file
ef-miglist-ocr
ef-miglist-operations
ef-miglist-all
```

## Ghi chú
- `add` chỉ áp dụng **module đơn** (`iam|document|file|ocr|operations`). Các hành động còn lại hỗ trợ `all`.
- SQL script xuất vào `deploy/artifacts/ef-<module>.sql`.
- Dùng `-Configuration Release` khi cần build Release.
- Sửa `ecm.settings.json` nếu thay đổi cấu trúc dự án.
- Script IAM (`database/01_iam.sql`) seed sẵn các group hệ thống `guest`, `system` và `Guess User`. Khi migrate trên môi trường mới hãy giữ nguyên các bản ghi này để user mới có quyền mặc định và `primary_group_id` hợp lệ.

## Chuyển đổi `department` sang unit group

Migration `20251101050000_RemoveDepartmentFromUsers` và `20251102060000_AddPrimaryGroupToUsers` xử lý toàn bộ quá trình chuyển đổi:

1. **Chạy script idempotent** (khuyến nghị chạy trong CI/CD trước khi `ef update`):

   ```sql
   DO $IAM$
   BEGIN
       IF NOT EXISTS (SELECT 1 FROM pg_namespace WHERE nspname = 'iam') THEN
           CREATE SCHEMA iam;
       END IF;
   END $IAM$;

   BEGIN;

   ALTER TABLE iam.users
       ADD COLUMN IF NOT EXISTS primary_group_id uuid;

   WITH unit_groups AS (
       SELECT DISTINCT TRIM(department) AS name
       FROM iam.users
       WHERE department IS NOT NULL AND TRIM(department) <> ''
   )
   INSERT INTO iam.groups (id, name, kind, created_at)
   SELECT uuid_generate_v4(), ug.name, 'unit', now()
   FROM unit_groups ug
   WHERE NOT EXISTS (
       SELECT 1 FROM iam.groups g WHERE g.name = ug.name
   );

   WITH user_departments AS (
       SELECT id AS user_id, TRIM(department) AS name
       FROM iam.users
       WHERE department IS NOT NULL AND TRIM(department) <> ''
   ),
   resolved_groups AS (
       SELECT ud.user_id, g.id AS group_id
       FROM user_departments ud
       JOIN iam.groups g ON g.name = ud.name
   )
   INSERT INTO iam.group_members (group_id, user_id, role, valid_from, valid_to)
   SELECT rg.group_id, rg.user_id, 'member', now(), NULL
   FROM resolved_groups rg
   ON CONFLICT (group_id, user_id)
   DO UPDATE SET role = EXCLUDED.role, valid_from = EXCLUDED.valid_from, valid_to = NULL;

   WITH user_departments AS (
       SELECT id AS user_id, TRIM(department) AS name
       FROM iam.users
       WHERE department IS NOT NULL AND TRIM(department) <> ''
   ),
   resolved_groups AS (
       SELECT ud.user_id, g.id AS group_id
       FROM user_departments ud
       JOIN iam.groups g ON g.name = ud.name
   )
   UPDATE iam.users u
   SET primary_group_id = COALESCE(u.primary_group_id, rg.group_id)
   FROM resolved_groups rg
   WHERE u.id = rg.user_id
     AND (u.primary_group_id IS NULL OR u.primary_group_id = rg.group_id);

   ALTER TABLE iam.users
       DROP COLUMN IF EXISTS department;

   ALTER TABLE iam.groups
       ALTER COLUMN kind SET DEFAULT 'temporary';

   CREATE UNIQUE INDEX IF NOT EXISTS ix_users_email ON iam.users (email);
   CREATE UNIQUE INDEX IF NOT EXISTS ix_roles_name ON iam.roles (name);
   CREATE UNIQUE INDEX IF NOT EXISTS ix_groups_name ON iam.groups (name);

   COMMIT;
   ```

   Script trên idempotent nên có thể chạy nhiều lần. Bạn có thể lưu tạm ra file khi triển khai (ví dụ `psql -f ./tmp/iam-forward.sql`) nhưng không cần commit vào repo.

2. **Áp dụng migration** trên môi trường cần nâng cấp:

   ```powershell
   .\ecm.ps1 ef update iam
   ```

   Hoặc chạy trực tiếp từ root repo:

   ```bash
   dotnet ef database update \
     --project src/Modules/IAM/ECM.IAM.csproj \
     --startup-project src/ECM/ECM.Host/ECM.Host.csproj \
     --context ECM.IAM.Infrastructure.Persistence.IamDbContext
   ```

3. **Xác minh dữ liệu** sau khi migrate:

   ```sql
   -- Liệt kê unit group vừa tạo
   SELECT id, name, kind FROM iam.groups WHERE kind = 'unit';

   -- Kiểm tra người dùng đã có primary_group_id và membership tương ứng
   SELECT u.email, u.primary_group_id, gm.group_id
   FROM iam.users u
   LEFT JOIN iam.group_members gm ON gm.user_id = u.id AND gm.group_id = u.primary_group_id
   ORDER BY u.email;
   ```

4. **Cập nhật client/API**: mọi truy vấn trước đây sử dụng `department` phải chuyển sang `group_id` hoặc `group_ids`. Ví dụ: `GET /documents?group_ids=<uuid>`.

Các môi trường đã bỏ cột `department` nhưng chưa tạo unit group có thể chạy lại script SQL forward (bước 1) vì logic `INSERT ... ON CONFLICT` đảm bảo idempotent.
