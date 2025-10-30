# ECM Change Log

## 2025-11-02 — Unit groups replace departments

**Impact:** Breaking change đối với IAM và các API lọc theo đơn vị.

- Loại bỏ cột `department` khỏi `iam.users`, bổ sung `primary_group_id` và unit group (`kind = 'unit'`).
- API `GET /documents`, `GET /search`, `POST /documents` hỗ trợ `group_id` và `group_ids[]` thay cho `department`.
- IAM provisioning yêu cầu truyền `primaryGroupId`/`groupIds[]` nếu hệ thống cần gán đơn vị ngay khi tạo user.
- Đã cập nhật tài liệu (`README.md`, `ARCHITECT.md`, `docs/api-reference.md`, `MIGRATION_GUIDE.md`) để phản ánh thay đổi.
- Quy trình migration trong `MIGRATION_GUIDE.md` kèm SQL idempotent giúp chuyển đổi dữ liệu `department` hiện hữu sang unit group.

**Hành động đề xuất cho client team:**

1. Cập nhật payload/contract để gửi `primaryGroupId` khi tạo user mới.
2. Sử dụng `group_id` hoặc `group_ids[]` trong mọi API filter (ví dụ `GET /documents?group_ids=<uuid>`).
3. Chạy migration IAM mới nhất theo hướng dẫn trong `MIGRATION_GUIDE.md` trước khi deploy phiên bản này.
4. Kiểm tra integration test phụ thuộc vào trường `department` và điều chỉnh mapping sang unit group.

Nếu cần hỗ trợ, liên hệ platform team qua channel `#ecm-upgrade`.
