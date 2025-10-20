# EF Tools – Hướng dẫn nhanh

Tất cả chạy từ **root repo** (nơi có `ecm.ps1`).

## Lệnh cơ bản

```powershell
# Tạo migration (module đơn lẻ)
.\ecm.ps1 ef add iam InitIamDb
.\ecm.ps1 ef add document AddIndexes
.\ecm.ps1 ef add file InitFileDb
.\ecm.ps1 ef add outbox InitOutbox

# Apply DB (module đơn lẻ hoặc all)
.\ecm.ps1 ef update iam
.\ecm.ps1 ef update outbox
.\ecm.ps1 ef update all

# Rollback về migration đích ("0" = trống)
.\ecm.ps1 ef rollback document -name 0
.\ecm.ps1 ef rollback iam -name 20251017_AddUserIndex
.\ecm.ps1 ef rollback outbox -name 0
.\ecm.ps1 ef rollback all -name 0

# Generate SQL (per module hoặc all)
.\ecm.ps1 ef script iam
.\ecm.ps1 ef script file -from 20251010_Init -to 20251017_AddX -idempotent
.\ecm.ps1 ef script outbox -idempotent
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
ef-outbox-add InitOutbox

# UPDATE
ef-iam-update
ef-doc-update
ef-file-update
ef-outbox-update
ef-update-all

# ROLLBACK
ef-iam-rollback 0
ef-doc-rollback 20251017_AddX
ef-file-rollback 0
ef-outbox-rollback 0
ef-rollback-all 0

# SCRIPT
ef-iam-script
ef-doc-script -From 20251010_Init -To 20251017_AddX -Idempotent
ef-file-script -Idempotent
ef-outbox-script -Idempotent
ef-script-all -Idempotent

# LIST
ef-list
ef-miglist-outbox
ef-miglist-all
```

## Ghi chú
- `add` chỉ áp dụng **module đơn** (`iam|document|file`). Các hành động còn lại hỗ trợ `all`.
- SQL script xuất vào `deploy/artifacts/ef-<module>.sql`.
- Dùng `-Configuration Release` khi cần build Release.
- Sửa `ecm.settings.json` nếu thay đổi cấu trúc dự án.
