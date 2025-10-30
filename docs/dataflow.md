# Bảng Dataflow ECM

Tài liệu này mô tả luồng dữ liệu từ các tác nhân (user, gateway, dịch vụ nền) tới những module trong ECM Monolith và chỉ ra bảng cơ sở dữ liệu nào được ghi nhận ở từng bước. Các bảng được trình bày bằng HTML để có thể sử dụng thuộc tính rowspan/colspan linh hoạt.

## Tác nhân và module
- **User/Browser**: người dùng cuối tương tác với giao diện SPA.
- **App Gateway**: BFF ASP.NET Core chịu trách nhiệm xác thực OIDC, phục vụ UI và forward request tới ECM.
- **Azure Entra ID**: Identity Provider phát hành token đăng nhập.
- **ECM Monolith**: bao gồm các module IAM, Document, File, Workflow, Signature, SearchRead, Ocr cùng hạ tầng chung.
- **Workers**: OutboxDispatcher, SearchIndexer, Notify, Ocr phối hợp xử lý các tác vụ bất đồng bộ.

## Bảng luồng nghiệp vụ
<table>
  <thead>
    <tr>
      <th>Luồng</th>
      <th>Giai đoạn</th>
      <th>Tác nhân / Service</th>
      <th>Module ECM</th>
      <th>Hành động &amp; Dữ liệu</th>
      <th>Sản phẩm dữ liệu</th>
    </tr>
  </thead>
  <tbody>
    <tr>
      <th rowspan="4">1. Đăng nhập &amp; bootstrap phiên</th>
      <td>1</td>
      <td>Browser → App Gateway</td>
      <td>Gateway (ngoài ECM)</td>
      <td>Người dùng mở <code>/signin-azure</code>; Gateway phục vụ SPA và khởi tạo OIDC.</td>
      <td>Redirect sang Azure, chưa ghi DB.</td>
    </tr>
    <tr>
      <td>2</td>
      <td>App Gateway ↔ Azure Entra ID</td>
      <td>IdP</td>
      <td>Azure xác thực, trả token chứa email/tên, chuyển hướng lại Gateway.</td>
      <td>Cookie đăng nhập và token OIDC.</td>
    </tr>
    <tr>
      <td>3</td>
      <td>App Gateway → ECM API</td>
      <td>IAM</td>
      <td>Gateway gọi <code>GET /api/iam/profile</code> để lấy hồ sơ dựa trên email token.</td>
      <td>Đọc <code>iam.users</code>.</td>
    </tr>
    <tr>
      <td>4</td>
      <td>IAM service</td>
      <td>IAM</td>
      <td>Provision/cập nhật tài khoản qua <code>POST /api/iam/users</code>, gán group &amp; role.</td>
      <td>Ghi <code>iam.users</code>, <code>iam.group_members</code>, <code>iam.user_roles</code>.</td>
    </tr>
    <tr>
      <th rowspan="4">2. Tạo tài liệu &amp; upload phiên bản</th>
      <td>1</td>
      <td>SPA → App Gateway</td>
      <td>Gateway</td>
      <td>Gửi <code>POST /documents</code> với metadata cơ bản/file khởi tạo.</td>
      <td>Request nội bộ tới ECM.</td>
    </tr>
    <tr>
      <td>2</td>
      <td>Gateway → ECM</td>
      <td>Document</td>
      <td>Document module tạo bản ghi tài liệu, suy luận metadata mặc định.</td>
      <td>Ghi <code>doc.document</code>.</td>
    </tr>
    <tr>
      <td>3</td>
      <td>Document module</td>
      <td>Document</td>
      <td>Lưu metadata JSON tùy biến.</td>
      <td>Ghi <code>doc.metadata</code>.</td>
    </tr>
    <tr>
      <td>4</td>
      <td>Gateway → ECM</td>
      <td>File</td>
      <td>Khởi tạo &amp; hoàn tất upload phiên bản qua <code>/documents/{id}/versions:init|complete</code>.</td>
      <td>Ghi <code>doc.version</code>, <code>doc.file_object</code>.</td>
    </tr>
    <tr>
      <th rowspan="3">3. Metadata &amp; tagging</th>
      <td>1</td>
      <td>SPA → Gateway</td>
      <td>Document</td>
      <td>Gọi <code>PUT /documents/{id}/metadata</code> cập nhật metadata key-value.</td>
      <td>Request nội bộ.</td>
    </tr>
    <tr>
      <td>2</td>
      <td>Document module</td>
      <td>Document</td>
      <td>Áp dụng metadata mới, cập nhật lịch sử.</td>
      <td>Ghi <code>doc.metadata</code>.</td>
    </tr>
    <tr>
      <td>3</td>
      <td>Document module</td>
      <td>Document</td>
      <td>Quản lý tag/folder qua các API tag.</td>
      <td>Ghi <code>doc.tag_namespace</code>, <code>doc.tag_label</code>, <code>doc.document_tag</code>.</td>
    </tr>
    <tr>
      <th rowspan="3">4. Chia sẻ tài liệu</th>
      <td>1</td>
      <td>SPA → Gateway</td>
      <td>File</td>
      <td>Tạo link chia sẻ hoặc quan hệ bằng <code>POST /files/share/{versionId}</code>, <code>/links</code>.</td>
      <td>Request nội bộ.</td>
    </tr>
    <tr>
      <td>2</td>
      <td>Gateway → ECM</td>
      <td>File (Share)</td>
      <td>File module áp chính sách link (hạn dùng, mật khẩu, quyền).</td>
      <td>Ghi bản ghi share.</td>
    </tr>
    <tr>
      <td>3</td>
      <td>File module</td>
      <td>File (Share)</td>
      <td>Lưu link, sự kiện truy cập và thống kê.</td>
      <td><code>file.share_link</code>, <code>file.share_access_event</code>, <code>file.share_stats</code>.</td>
    </tr>
    <tr>
      <th rowspan="3">5. Workflow &amp; form</th>
      <td>1</td>
      <td>SPA → Gateway</td>
      <td>Workflow</td>
      <td>Khởi chạy workflow <code>POST /wf/instances</code>, thao tác nhiệm vụ qua <code>/wf/tasks</code>.</td>
      <td>Request nội bộ.</td>
    </tr>
    <tr>
      <td>2</td>
      <td>Gateway → ECM</td>
      <td>Workflow</td>
      <td>Workflow module tạo instance, phân nhiệm vụ, render form.</td>
      <td>Ghi <code>wf.instance</code>, <code>wf.task</code>, <code>wf.form</code>.</td>
    </tr>
    <tr>
      <td>3</td>
      <td>Workflow module</td>
      <td>Workflow</td>
      <td>Lưu dữ liệu form gắn tài liệu/instance.</td>
      <td>Ghi <code>wf.form_data</code>.</td>
    </tr>
    <tr>
      <th rowspan="3">6. Tìm kiếm &amp; chỉ mục</th>
      <td>1</td>
      <td>SPA → Gateway</td>
      <td>SearchRead</td>
      <td>Gọi <code>GET /search</code>, <code>/search/suggest</code>, <code>/search/facets</code>.</td>
      <td>Query nội bộ.</td>
    </tr>
    <tr>
      <td>2</td>
      <td>Gateway → ECM</td>
      <td>SearchRead</td>
      <td>SearchRead module đọc chỉ mục văn bản, KV, vector.</td>
      <td>Sử dụng <code>search.fts</code>, <code>search.kv</code>, <code>search.embedding</code>.</td>
    </tr>
    <tr>
      <td>3</td>
      <td>SearchIndexer worker</td>
      <td>SearchRead</td>
      <td>Tiêu thụ Outbox để đồng bộ chỉ mục.</td>
      <td>Đọc <code>ops.outbox</code>, ghi schema <code>search</code>.</td>
    </tr>
    <tr>
      <th rowspan="3">7. OCR &amp; trích xuất</th>
      <td>1</td>
      <td>SPA → Gateway</td>
      <td>Ocr</td>
      <td>Yêu cầu xử lý OCR bằng <code>POST /ocr/process</code>, tra cứu kết quả và annotation.</td>
      <td>Request tới ECM/OCR service.</td>
    </tr>
    <tr>
      <td>2</td>
      <td>Gateway → ECM/OCR worker</td>
      <td>Ocr</td>
      <td>Ocr module và worker kích hoạt engine Python, phát sự kiện.</td>
      <td>Sự kiện vào hàng đợi/Outbox.</td>
    </tr>
    <tr>
      <td>3</td>
      <td>Ocr module</td>
      <td>Ocr</td>
      <td>Lưu kết quả trang, annotation, dữ liệu trích xuất.</td>
      <td><code>ocr.result</code>, <code>ocr.page_text</code>, <code>ocr.annotation</code>, <code>ocr.extraction</code>.</td>
    </tr>
    <tr>
      <th rowspan="3">8. Audit, thông báo &amp; retention</th>
      <td>1</td>
      <td>SPA/Worker</td>
      <td>Shared</td>
      <td>Tra cứu audit, retention qua <code>GET /audit</code>, <code>/retention/policies</code>.</td>
      <td>Request nội bộ.</td>
    </tr>
    <tr>
      <td>2</td>
      <td>Các module nghiệp vụ</td>
      <td>Document/Workflow/File...</td>
      <td>Phát sự kiện domain vào Outbox, ghi audit.</td>
      <td>Ghi <code>ops.outbox</code>, <code>ops.audit_event</code>.</td>
    </tr>
    <tr>
      <td>3</td>
      <td>Notify/Retention workers</td>
      <td>Shared</td>
      <td>Gửi thông báo, đánh dấu ứng viên retention, webhooks.</td>
      <td>Ghi <code>ops.notification</code>, <code>ops.retention_policy</code>, <code>ops.retention_candidate</code>, <code>ops.webhook*</code>.</td>
    </tr>
  </tbody>
</table>

## Bảng nguồn dữ liệu
<table>
  <thead>
    <tr>
      <th>Bảng &amp; Cột</th>
      <th>Module</th>
      <th>Nguồn sinh dữ liệu</th>
    </tr>
  </thead>
  <tbody>
    <tr>
      <td><code>iam.users</code> (email, display_name, primary_group_id, is_active, created_at)<br><code>iam.user_roles</code><br><code>iam.group_members</code></td>
      <td>IAM</td>
      <td>Provision qua <code>POST /api/iam/users</code>, gán role/group mặc định.</td>
    </tr>
    <tr>
      <td><code>iam.relations</code> (subject_*, relation, valid_from, valid_to)</td>
      <td>IAM</td>
      <td>Thiết lập chia sẻ/ReBAC bằng <code>POST /api/iam/relations</code>.</td>
    </tr>
    <tr>
      <td><code>doc.document</code> (title, doc_type, status, sensitivity, owner_id, group_id, created_by, type_id, timestamps)</td>
      <td>Document</td>
      <td>Tạo tài liệu bằng <code>POST /documents</code>; module suy luận metadata mặc định.</td>
    </tr>
    <tr>
      <td><code>doc.version</code> (storage_key, bytes, mime_type, sha256, version_no, created_by, created_at)<br><code>doc.file_object</code> (storage_key, legal_hold, created_at)</td>
      <td>File</td>
      <td>Quy trình upload phiên bản (<code>/documents/{id}/versions:init|complete</code>) quản lý lưu trữ MinIO/S3.</td>
    </tr>
    <tr>
      <td><code>doc.metadata</code> (data)<br><code>doc.effective_acl_flat</code> (user_id, valid_to, source, idempotency_key)</td>
      <td>Document</td>
      <td>Cập nhật metadata qua <code>PUT /documents/{id}/metadata</code>; worker ACL vật hoá quyền truy cập.</td>
    </tr>
    <tr>
      <td><code>doc.tag_namespace</code>, <code>doc.tag_label</code>, <code>doc.document_tag</code></td>
      <td>Document</td>
      <td>API tag/folder (<code>/tags/*</code>, <code>/documents/{id}/tags</code>) ghi nhận namespace, label và gán tag.</td>
    </tr>
    <tr>
      <td><code>file.share_link</code>, <code>file.share_access_event</code>, <code>file.share_stats</code></td>
      <td>File (Share)</td>
      <td>Tạo link/quyền chia sẻ qua <code>POST /links</code>, <code>/files/share/{versionId}</code> và log truy cập.</td>
    </tr>
    <tr>
      <td><code>wf.definition</code>, <code>wf.instance</code>, <code>wf.task</code>, <code>wf.form</code>, <code>wf.form_data</code></td>
      <td>Workflow</td>
      <td>Định nghĩa, khởi chạy workflow và lưu form qua các API <code>/wf/definitions</code>, <code>/wf/instances</code>, <code>/wf/tasks</code>, <code>/forms*</code>.</td>
    </tr>
    <tr>
      <td><code>search.fts</code>, <code>search.kv</code>, <code>search.embedding</code></td>
      <td>SearchRead</td>
      <td>SearchIndexer tiêu thụ Outbox để xây dựng chỉ mục phục vụ <code>/search</code>.</td>
    </tr>
    <tr>
      <td><code>ocr.result</code>, <code>ocr.page_text</code>, <code>ocr.annotation</code>, <code>ocr.extraction</code></td>
      <td>Ocr</td>
      <td>Quy trình OCR từ API <code>/ocr/process</code> và workers lưu kết quả.</td>
    </tr>
    <tr>
      <td><code>ops.outbox</code>, <code>ops.outbox_deadletter</code>, <code>ops.audit_event</code>, <code>ops.notification</code>, <code>ops.retention_policy</code>, <code>ops.retention_candidate</code>, <code>ops.webhook*</code></td>
      <td>Shared (Notify/Retention)</td>
      <td>Các module ghi sự kiện vào Outbox, audit; workers gửi thông báo, quản lý retention và webhook.</td>
    </tr>
  </tbody>
</table>
