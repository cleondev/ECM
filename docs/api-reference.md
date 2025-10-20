# ECM API Reference Overview

Tài liệu này tổng hợp các API mới của hệ thống ECM theo từng nhóm chức năng. Mỗi bảng bên dưới tuân theo định dạng **METHOD /path – mô tả – tham số chính** để có thể chuyển đổi nhanh vào tài liệu OpenAPI hoặc dùng trong quá trình phát triển front-end.

> **Lưu ý chung**
>
> * Các tham số phân trang thống nhất: `page` (mặc định `1`) và `pageSize` (mặc định `24`).
> * Các API liệt kê hỗ trợ tham số `sort` theo cú pháp `field:asc,field2:desc` trừ khi ghi chú khác.
> * Tham số `q` là tìm kiếm toàn văn trên tên hoặc tiêu đề đối tượng tương ứng.

## 1. IAM

### Users

| Method & Path | Mô tả | Tham số chính |
| --- | --- | --- |
| `GET /api/iam/users` | Liệt kê người dùng trong module IAM. | – |
| `GET /api/iam/users/{id}` | Chi tiết người dùng theo ID. | `id` |
| `GET /api/iam/users/by-email` | Tìm người dùng theo email. | Query `email` |
| `POST /api/iam/users` | Tạo người dùng mới và gán vai trò mặc định. | Body `{email, displayName, department?, isActive?, roleIds[]}` |
| `PUT /api/iam/users/{id}` | Cập nhật hồ sơ cơ bản hoặc trạng thái hoạt động. | `id`, body `{displayName, department?, isActive?}` |
| `POST /api/iam/users/{id}/roles` | Gán thêm một vai trò cho người dùng. | `id`, body `{roleId}` |
| `DELETE /api/iam/users/{id}/roles/{roleId}` | Hủy gán vai trò khỏi người dùng. | `id`, `roleId` |

### Profile

| Method & Path | Mô tả | Tham số chính |
| --- | --- | --- |
| `GET /api/iam/profile` | Lấy hồ sơ của người dùng đang đăng nhập (theo email trong token). | – |
| `PUT /api/iam/profile` | Cập nhật hồ sơ cá nhân (tên hiển thị, phòng ban). | Body `{displayName, department?}` |

### Roles

| Method & Path | Mô tả | Tham số chính |
| --- | --- | --- |
| `GET /api/iam/roles` | Danh sách vai trò. | – |
| `GET /api/iam/roles/{id}` | Chi tiết vai trò theo ID. | `id` |
| `POST /api/iam/roles` | Tạo vai trò mới. | Body `{name}` |
| `PUT /api/iam/roles/{id}` | Đổi tên vai trò. | `id`, body `{name}` |
| `DELETE /api/iam/roles/{id}` | Xóa vai trò. | `id` |

### Relations (ReBAC)

| Method & Path | Mô tả | Tham số chính |
| --- | --- | --- |
| `GET /api/iam/relations/subjects/{subjectId}` | Liệt kê quan hệ truy cập theo subject. | `subjectId` |
| `GET /api/iam/relations/objects/{objectType}/{objectId}` | Liệt kê quan hệ truy cập theo object. | `objectType`, `objectId` |
| `POST /api/iam/relations` | Tạo quan hệ truy cập mới. | Body `{subjectId, objectType, objectId, relation}` |
| `DELETE /api/iam/relations/subjects/{subjectId}/objects/{objectType}/{objectId}` | Xóa quan hệ truy cập. | `subjectId`, `objectType`, `objectId`, query `relation` |

## 2. Share (ReBAC)

| Method & Path | Mô tả | Tham số chính |
| --- | --- | --- |
| `GET /share/relations` | Truy vấn quan hệ chia sẻ (ReBAC). | `object_type`, `object_id`, `page`, `pageSize` |
| `POST /share/relations` | Tạo quan hệ chia sẻ. | Body `{subject_id, subject_type, object_type, object_id, relation}` |
| `DELETE /share/relations` | Xóa quan hệ chia sẻ. | Body giống `POST` |
| `POST /links` | Tạo liên kết chia sẻ tạm thời. | Body `{object, object_id, expires_in, password?}` |
| `GET /links/{id}` | Lấy metadata của liên kết chia sẻ. | `id` |
| `DELETE /links/{id}` | Hủy liên kết chia sẻ. | `id` |

## 3. Tags & Folders

| Method & Path | Mô tả | Tham số chính |
| --- | --- | --- |
| `GET /tags/namespaces` | Danh sách namespace tag. | `q`, `page`, `pageSize` |
| `POST /tags/namespaces` | Tạo namespace tag. | Body namespace |
| `PATCH /tags/namespaces/{slug}` | Cập nhật namespace. | `slug`, body |
| `DELETE /tags/namespaces/{slug}` | Xóa namespace. | `slug` |
| `GET /tags/{namespace}/labels` | Danh sách label thuộc namespace. | `namespace`, `q`, `page`, `pageSize`, `parent?`, `active?` |
| `POST /tags/{namespace}/labels` | Tạo label mới. | `namespace`, body |
| `PATCH /tags/{namespace}/labels/{id}` | Cập nhật label. | `namespace`, `id`, body |
| `DELETE /tags/{namespace}/labels/{id}` | Xóa label. | `namespace`, `id` |
| `GET /documents/{id}/tags` | Lấy tag của tài liệu. | `id` |
| `POST /documents/{id}/tags` | Gán tag cho tài liệu. | `id`, body `{tag_id}` |
| `DELETE /documents/{id}/tags/{tagId}` | Bỏ gán tag khỏi tài liệu. | `id`, `tagId` |
| `GET /facets/tags` | Thống kê tài liệu theo tag. | `q`, `folder`, `doc_type`, `status`, `sensitivity` |
| `GET /folders` | Danh sách thư mục hệ thống. | – |
| `GET /folders/{name}/documents` | Liệt kê tài liệu trong thư mục. | `name`, `page`, `pageSize`, `sort`, `q` |

## 4. Document (Cards & Metadata)

| Method & Path | Mô tả | Tham số chính |
| --- | --- | --- |
| `GET /documents` | Liệt kê tài liệu theo bộ lọc. | `q`, `page`, `pageSize`, `sort`, `doc_type`, `status`, `sensitivity`, `owner_id`, `dept`, `tags[]` |
| `POST /documents` | Tạo tài liệu mới. | Body `{title, doc_type, type_id?, sensitivity, department, metadata?}` |
| `GET /documents/{id}` | Chi tiết tài liệu (owner, badges, version). | `id` |
| `PATCH /documents/{id}` | Cập nhật thông tin cơ bản. | `id`, body |
| `DELETE /documents/{id}` | Xóa mềm (mặc định) hoặc xóa hẳn khi `hard=true`. | `id`, query `hard?` |
| `GET /documents/{id}/metadata` | Lấy metadata dạng key-value. | `id` |
| `PUT /documents/{id}/metadata` | Ghi đè metadata. | `id`, body `{data}` |
| `GET /documents/{id}/history` | Lịch sử thay đổi thuộc tính. | `id`, `page`, `pageSize` |
| `PUT /documents/{id}/folder` | Cập nhật thư mục chứa tài liệu. | `id`, body `{folder}` |

## 5. Document Types

| Method & Path | Mô tả | Tham số chính |
| --- | --- | --- |
| `GET /document-types` | Danh sách loại tài liệu. | `page`, `pageSize`, `active?` |
| `POST /document-types` | Tạo loại tài liệu. | Body loại tài liệu |
| `PATCH /document-types/{id}` | Cập nhật loại tài liệu. | `id`, body |
| `DELETE /document-types/{id}` | Xóa loại tài liệu. | `id` |

## 6. Versions & Files

| Method & Path | Mô tả | Tham số chính |
| --- | --- | --- |
| `GET /documents/{id}/versions` | Liệt kê phiên bản tài liệu. | `id`, `page`, `pageSize` |
| `POST /documents/{id}/versions:init` | Khởi tạo upload phiên bản mới, trả presigned URL. | `id`, body `{mime_type, bytes, sha256}` |
| `POST /documents/{id}/versions/{versionId}/complete` | Hoàn tất upload phiên bản. | `id`, `versionId` |
| `GET /versions/{versionId}` | Chi tiết phiên bản. | `versionId` |
| `DELETE /versions/{versionId}` | Xóa phiên bản (theo policy). | `versionId` |
| `POST /versions/{versionId}/promote` | Đặt phiên bản làm hiện hành. | `versionId` |
| `GET /files/download/{versionId}` | Tải file (redirect signed URL). | `versionId` |
| `GET /files/preview/{versionId}` | Stream preview (PDF/image/video). | `versionId` |
| `GET /files/thumbnails/{versionId}` | Lấy thumbnail. | `versionId`, `w`, `h`, `fit=cover|contain` |

## 7. Workflow

| Method & Path | Mô tả | Tham số chính |
| --- | --- | --- |
| `GET /wf/definitions` | Danh sách định nghĩa workflow. | `page`, `pageSize`, `active?` |
| `POST /wf/definitions` | Tạo định nghĩa workflow. | Body `{name, spec}` |
| `GET /wf/definitions/{id}` | Chi tiết định nghĩa. | `id` |
| `PATCH /wf/definitions/{id}` | Cập nhật định nghĩa. | `id`, body |
| `DELETE /wf/definitions/{id}` | Xóa định nghĩa. | `id` |
| `POST /wf/instances` | Khởi chạy workflow cho tài liệu. | Body `{definition_id, document_id, variables?}` |
| `GET /wf/instances` | Liệt kê phiên chạy workflow. | `document_id`, `state`, `created_by`, `page`, `pageSize` |
| `GET /wf/instances/{id}` | Chi tiết trạng thái workflow. | `id` |
| `POST /wf/instances/{id}/cancel` | Hủy workflow. | `id`, body `{reason?}` |
| `GET /wf/tasks` | Nhiệm vụ theo người dùng. | `assignee_id=me|uuid`, `state=open|done`, `document_id?`, `page`, `pageSize` |
| `GET /wf/tasks/{id}` | Chi tiết nhiệm vụ (form, variables). | `id` |
| `POST /wf/tasks/{id}/claim` | Nhận xử lý nhiệm vụ. | `id` |
| `POST /wf/tasks/{id}/complete` | Hoàn tất nhiệm vụ với hành động. | `id`, body `{action, comment, outputs?}` |
| `POST /wf/tasks/{id}/reassign` | Chuyển giao nhiệm vụ. | `id`, body `{assignee_id}` |

## 8. Dynamic Forms

| Method & Path | Mô tả | Tham số chính |
| --- | --- | --- |
| `GET /forms` | Liệt kê form động. | `page`, `pageSize`, `q`, `active?` |
| `POST /forms` | Tạo form. | Body `{name, schema_json, ui_schema?}` |
| `GET /forms/{id}` | Chi tiết form. | `id` |
| `PATCH /forms/{id}` | Cập nhật form. | `id`, body |
| `DELETE /forms/{id}` | Xóa form. | `id` |
| `GET /forms/data` | Tìm dữ liệu form đã lưu. | `form_id?`, `instance_id?`, `document_id?`, `page`, `pageSize` |
| `POST /forms/data` | Upsert dữ liệu form. | Body `{form_id, instance_id?, document_id?, data}` |
| `DELETE /forms/data/{id}` | Xóa dữ liệu form. | `id` |

## 9. Search (FTS / Vector / Hybrid)

| Method & Path | Mô tả | Tham số chính |
| --- | --- | --- |
| `GET /search` | Tìm kiếm tài liệu. | `q`, `mode=fts|vector|hybrid`, `doc_type`, `status`, `sensitivity`, `owner_id`, `dept`, `tags[]`, `page`, `pageSize`, `sort` |
| `GET /search/suggest` | Autocomplete gợi ý. | `q`, `limit?` |
| `GET /search/facets` | Thống kê facet. | `q`, `doc_type`, `status`, `sensitivity`, `owner_id`, `dept`, `tags[]` |

## 10. OCR

| Method & Path | Mô tả | Tham số chính |
| --- | --- | --- |
| `POST /ocr/process` | Kích hoạt xử lý OCR cho phiên bản. | Body `{document_id, version_id, force?}` |
| `GET /ocr/result` | Trạng thái kết quả OCR. | `document_id`, `version_id` |
| `GET /ocr/pages` | Lấy văn bản theo trang. | `document_id`, `version_id`, `page`, `pageSize` |
| `GET /ocr/annotations` | Danh sách annotation OCR. | `document_id`, `version_id`, `page`, `pageSize` |
| `POST /ocr/annotations` | Lưu annotation thủ công. | Body annotation |
| `DELETE /ocr/annotations/{id}` | Xóa annotation. | `id` |
| `GET /ocr/extractions` | Xem dữ liệu trường đã trích xuất. | `document_id`, `version_id` |

## 11. Audit, Activity & Retention

| Method & Path | Mô tả | Tham số chính |
| --- | --- | --- |
| `GET /audit` | Tra cứu nhật ký hành động. | `object_type`, `object_id`, `page`, `pageSize`, `actor?`, `action?` |
| `GET /retention/policies` | Danh sách chính sách lưu trữ. | `page`, `pageSize`, `q`, `active?` |
| `POST /retention/policies` | Tạo chính sách lưu trữ. | Body policy |
| `PATCH /retention/policies/{id}` | Cập nhật chính sách lưu trữ. | `id`, body |
| `DELETE /retention/policies/{id}` | Xóa chính sách lưu trữ. | `id` |
| `GET /retention/candidates` | Danh sách đối tượng sắp hết hạn. | `due_before`, `page`, `pageSize` |
| `POST /retention/execute` | Thực thi policy (hủy hoặc đóng hồ sơ). | Body `{policy_id?, document_ids?}` |

## 12. Notifications, Operations & Webhooks

| Method & Path | Mô tả | Tham số chính |
| --- | --- | --- |
| `GET /notifications` | Danh sách thông báo của người dùng. | `page`, `pageSize`, `unread?` |
| `POST /notifications/{id}/read` | Đánh dấu đã đọc. | `id` |
| `GET /webhooks` | Danh sách webhook đã đăng ký. | `page`, `pageSize` |
| `POST /webhooks` | Đăng ký webhook mới. | Body `{event_types[], url, secret}` |
| `DELETE /webhooks/{id}` | Gỡ webhook. | `id` |
| `GET /outbox/events` | Tra cứu sự kiện đẩy đi (debug/admin). | `type?`, `since?`, `page`, `pageSize` |

## 13. System

| Method & Path | Mô tả | Tham số chính |
| --- | --- | --- |
| `GET /health` | Kiểm tra sức khỏe hệ thống và phụ thuộc. | – |
| `GET /config/ui` | Lấy cấu hình UI (feature flags, limits). | – |
| `GET /stats/overview` | Số liệu tổng quan về tài liệu, dung lượng, tiến độ phê duyệt. | – |

---

### Ghi chú bổ sung

* Khi chuẩn hóa OpenAPI, nên mô tả rõ schema của từng payload (ví dụ cấu trúc body tạo người dùng, format của `metadata`).
* Với endpoint khởi tạo upload (`POST /documents/{id}/versions:init`), tùy công cụ OpenAPI có thể đổi thành `/documents/{id}/versions:initiate` hoặc `/documents/{id}/versions/init` để tránh ký tự đặc biệt.
* Các endpoint xóa (`DELETE`) nên ghi chú thêm hành vi (soft-delete, hard-delete) bằng extension `x-notes` hoặc mô tả chi tiết.
