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
- Script IAM (`database/01_iam.sql`) seed sẵn hai group hệ thống `guest` và `system`. Khi migrate trên môi trường mới hãy giữ nguyên các bản ghi này để user mới có quyền mặc định.
