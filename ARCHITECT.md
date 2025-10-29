# ARCHITECT.md

> **ECM – Enterprise Content Management System**  
> **Stack:** .NET 9 (services), Python (OCR), PostgreSQL 16, MinIO, Redpanda (Kafka-compatible), Docker Desktop Linux (DEV)  
> **Architecture:** Modular Monolith + Aspire + Event-driven (Outbox → Broker)

---

## 1) System Overview

```
[AppGateway] — UI + BFF + Reverse Proxy
       │
       ▼
[ECM] (Monolith)
   ├─ Document
   ├─ File (MinIO adapter)
   ├─ Workflow
   ├─ Signature
   ├─ SearchRead
   └─ Ocr (Dot OCR adapter + API)
        └─→ (bus.outbox → Redpanda)

Background Workers:
   - OutboxDispatcher  → publish domain events
   - SearchIndexer     → consume → build FTS/KV/Vector
   - Notify            → consume → email/webhook
   - Ocr              → trigger Dot OCR service, emit ocr.* events via external engine
   - OCR (Python)      → extract, label, emit ocr.* events
```

**Principles**

- Modular monolith: domain modules độc lập về code + schema.
- PostgreSQL schemas per module (`doc`, `file`, `wf`, `sign`, `search`, `ocr`).
- RLS kiểm soát quyền đọc theo RBAC/ReBAC/ABAC.
- Outbox pattern bảo đảm event reliable + idempotent consumers.
- Search hybrid (FTS + pgvector + KV).
- Aspire điều phối toàn bộ môi trường DEV.

---

## 2) Project Structure (Monolith + Aspire)

| Project / Folder | Lang              | Purpose                                                          | Key Components                                                     |
| ---------------- | ----------------- | ---------------------------------------------------------------- | ------------------------------------------------------------------ |
| **AppGateway**   | .NET 9 + JS       | Edge Gateway (BFF, Auth, serve UI)                               | `/ui` (SPA), reverse proxy, auth, BFF endpoints                    |
| **ECM**          | .NET 9            | Core nghiệp vụ (Document, File, Workflow, Signature, SearchRead, Ocr) | Modular monolith: Domain/Application/Infrastructure/Api per module |
| **Workers**      | .NET 9            | Background processing                                            | OutboxDispatcher, SearchIndexer, Notify, Ocr                       |
| **Ocr**          | Python            | OCR engine + labeling UI (Dot OCR service lives ngoài monolith)  | Tesseract/PaddleOCR, FastAPI, labeling tool                        |
| **Aspire**       | .NET 9            | Orchestration / Observability                                    | AppHost + ServiceDefaults                                          |
| **deploy**       | YAML              | Docker Compose (DEV infra)                                       | Postgres, MinIO, Redpanda                                          |
| **docs**         | Markdown / Drawio | Documentation                                                    | Architecture, API, Data model                                      |
| **tests**        | .NET 9 xUnit      | Unit + Integration Tests                                         | Per module + per worker                                            |

---

## 3) Detailed Module Layout

```
src/
├── AppGateway/
│   ├── AppGateway.Api/            # ASP.NET host (BFF + proxy + auth)
│   │   ├── Controllers/
│   │   ├── Middlewares/
│   │   ├── ReverseProxy/
│   │   ├── Auth/
│   │   ├── appsettings.json
│   │   └── Program.cs
│   ├── AppGateway.Contracts/
│   ├── AppGateway.Infrastructure/
│   └── ui/                        # SPA UI (React/Next/Vite)
│       ├── package.json
│       ├── public/
│       ├── src/
│       │   ├── app/
│       │   ├── features/
│       │   ├── services/
│       │   └── shared/
│       └── dist/                  # built UI (served under /app)
│
├── ECM/
│   ├── ECM.Host/                  # Host (IModule loader)
│   └── ECM.BuildingBlocks/        # Shared kernel, abstractions, outbox, events
├──  Modules/
│    ├── Document/{Domain,Application,Infrastructure,Api}
│    ├── File/{Domain,Application,Infrastructure,Api}
│    ├── Workflow/{Domain,Application,Infrastructure,Api}
│    ├── Signature/{Domain,Application,Infrastructure,Api}
│    ├── SearchRead/{Application,Infrastructure,Api}
│    └── Ocr/{Domain,Application,Infrastructure,Api}
│
├── Workers/
│   ├── OutboxDispatcher.Worker/
│   ├── SearchIndexer.Worker/
│   ├── Notify.Worker/
│   └── Ocr.Worker/
│
├── Ocr/
│   ├── ocr-engine/
│   └── labeling-ui/
│
├── Aspire/
│   └── ECM.AppHost/
│       ├── Program.cs
│       ├── appsettings.json
│       └── Properties/
│
└── Shared/
    ├── Contracts/
    ├── Messaging/
    ├── ServiceDefaults/
    ├── Utilities/
    └── Extensions/
```

---

## 4) Aspire Integration

**AppHost (DistributedApplication):**
**ServiceDefaults:**

- Add OpenTelemetry (Tracing, Metrics)
- HealthChecks, Resilience policy (HttpClient)
- Common logging, retry, discovery settings

---

## 5) Database Model Summary

Schemas:
`iam, doc, wf, search, ocr, ops`

### Core Tables

* `iam.users(id, email, display_name, is_active, created_at)` — người dùng hệ thống; quyền đơn vị nội bộ được suy ra từ thành viên nhóm `iam.group_members`

* `iam.roles(id, name)` — vai trò định nghĩa sẵn (Admin, Editor, Viewer, …)

* `iam.user_roles(user_id, role_id)` — ánh xạ người dùng ↔ vai trò (RBAC)

* `iam.groups(id, name, kind, created_by, created_at)` — nhóm động (team, workflow, tạm thời); migrations seed sẵn hai group hệ thống `guest` và `system` để gán quyền mặc định.
* `iam.group_members(group_id, user_id, role, valid_from, valid_to)` — thành viên nhóm với thời hạn hiệu lực; user mới được provisioning tự động gia nhập `guest` và `system` thông qua `DefaultGroupAssignmentService`.
* `iam.relations(subject_type, subject_id, object_type, object_id, relation, valid_from, valid_to)` — quan hệ ReBAC cho user/group với thời gian hiệu lực

* `doc.document(id, title, type_id, status, sensitivity, owner_id, created_at, updated_at)`

* `doc.version(id, document_id, storage_key, bytes, sha256, created_by)`
* `doc.effective_acl_flat(document_id, user_id, valid_to, source, idempotency_key, updated_at)` — read-model phẳng để truy vấn quyền truy cập theo user

* `doc.document_type(id, type_key, type_name, is_active)`

* `doc.metadata(document_id, data jsonb)`

* `doc.tag_namespace(namespace_slug, kind, owner_user_id)` / `doc.tag_label(id, namespace_slug, path)` / `doc.document_tag(document_id, tag_id)`

* `doc.signature_request(id, document_id, provider, status)` / `doc.signature_result(request_id, status, evidence_url)`

* `wf.definition / wf.instance / wf.task / wf.form / wf.form_data`

* `search.fts / search.kv / search.embedding`

* `ocr.result / ocr.page_text / ocr.annotation / ocr.extraction`

* `ops.outbox / ops.outbox_deadletter / ops.audit_event / ops.notification / ops.webhook / ops.webhook_delivery / ops.retention_policy / ops.retention_candidate`

RLS function `doc.fn_can_read_document(row)` = RBAC + ReBAC + ABAC (group). Worker materialize `doc.effective_acl_flat` để phục vụ API liệt kê tài liệu nhanh và chỉ trả quyền còn hiệu lực.


---

## 6) Events & Topics Summary

Modules ghi sự kiện vào `ops.outbox`; **Outbox Dispatcher** đọc và publish lên **Redpanda** theo topic.

### Topics

* `iam.events` – user/role thay đổi (tuỳ chọn)
* `document.events` – CRUD document, metadata, tag
* `version.events` – version mới → trigger OCR & SearchIndexer
* `workflow.events` – instance, task update → Notify, Audit
* `signature.events` – trạng thái ký số (completed/failed)
* `ocr.events` – OCR kết quả, extraction → SearchIndexer
* `search.events` – đồng bộ read model / rebuild index
* `audit.events` – log thao tác người dùng
* `retention.events` – lịch dọn dữ liệu, purge/archive

### Payload (chuẩn JSON)

```jsonc
{
  "eventId": "uuid",
  "type": "document.created",
  "aggregate": "document",
  "aggregateId": "uuid",
  "occurredAt": "2025-10-09T04:30:00Z",
  "actorId": "uuid",
  "data": { ... }
}
```

### Flow ví dụ

```
document.uploaded → worker-ocr → Dot OCR service → ocr.completed → worker-search-indexer
document.created → worker-search-indexer, worker-notify, audit
version.created → ocr-engine → ocr.extraction.updated → worker-search-indexer
workflow.task.assigned → worker-notify
signature.completed → audit
```

---

## 7) APIs (samples)

### Document

- `POST /documents` → create draft document (metadata JSONB)
- `POST /documents/{id}/versions` → presigned upload via file-service
- `GET /documents/{id}` (RLS enforced)
- `GET /documents?query=&filters=`

### Workflow

- `POST /workflows/definitions`
- `POST /workflows/instances` (document_id, definition_id)
- `POST /tasks/{id}/approve|reject`

### Search (hybrid)

- `GET /search?q=&mode=hybrid&filters=group_id:11111111-1111-1111-1111-111111111111,doc_type:Contract`
  - Combines FTS rank + cosine similarity from `search.embedding` + KV filters.

### OCR

- `GET /api/ocr/samples/{sampleId}/results` → proxy kết quả OCR của sample từ Dot OCR.
- `GET /api/ocr/samples/{sampleId}/boxes` → danh sách box/bounding box của sample.
- `GET /api/ocr/samples/{sampleId}/boxes/{boxId}/results` → kết quả chi tiết cho một boxing.
- `PUT /api/ocr/samples/{sampleId}/boxes/{boxId}` → cập nhật giá trị/nhãn cho box.
- `Ocr.Worker` (Kafka consumer) lắng nghe `ecm.document.uploaded` → gọi Dot OCR service khởi chạy xử lý.
- `src/Ocr/*` (Python) vẫn đảm nhiệm engine + labeling UI, phát hành `ocr.*` events khi hoàn tất.

---

## 8) Build & Run (DEV)

```bash
# 1) Prepare
cp .env.example .env   # fill values
# 2) Start infra + apps
docker compose -f deploy/compose.yml up -d --build
# 3) Check UIs
open http://localhost:9001   # MinIO console
open http://localhost:5050   # pgAdmin (if enabled)
open http://localhost:8089   # Redpanda console
open http://localhost:8080   # Gateway
```

**DB init**: put full DDL into `deploy/init/db-init.sql` (RLS + schemas).  
**Operations module (Outbox)**: dispatcher will publish events to topics; consumers update read models.

---

## 9) Security & RLS

- API layer sets:
  `SET LOCAL app.user_id = '<uuid>'; SET LOCAL app.user_group_id = '<uuid>';`
- `authz.fn_can_read(doc_row)` enforces combined RBAC/ReBAC/ABAC at DB layer.
- OCR service account can write `ocr.annotation`, resolver writes `ocr.extraction`, but **cannot** mutate user metadata directly.

---

## 10) Scaling & Split-by-Seams

- Split `search` API when QPS high or externalize to ES/VectorDB.
- `audit` to WORM/append-only storage when compliance requires.
- `signature` isolate with HSM/KMS.
- `retention` to dedicated job service for heavy purge/transition.

---

## 11) Testing & Observability

- Contract tests for REST & Event payloads (`search-index-contracts`).
- OpenTelemetry tracing; logs structured (JSON).
- Prometheus metrics for dispatcher backlog & consumer lag.
- Smoke tests: upload → version.created → OCR → extraction → search available.

---

## 12) Appendix – Example Event Payloads

```jsonc
// version.created
{
  "eventId": "uuid",
  "type": "version.created",
  "occurredAt": "2025-10-06T03:30:00Z",
  "documentId": "uuid",
  "versionId": "uuid",
  "versionNo": 2,
  "mime": "application/pdf",
  "bytes": 245812,
  "storageKey": "docs/.../v2/2025/10/06/file.pdf",
  "sha256": "HEX..."
}

// ocr.extraction.updated
{
  "eventId": "uuid",
  "type": "ocr.extraction.updated",
  "documentId": "uuid",
  "versionId": "uuid",
  "updatedFields": ["id_number","full_name"],
  "at": "2025-10-06T03:45:00Z"
}
```

---

### Notes for Codex/Agent

- Prefer generating service skeletons per project with minimal REST endpoints and consuming/producing event contracts above.
- Keep repository interfaces in `shared-interfaces`; infra adapters in specific packages.
- Respect RLS: all DB-aware code must set user context per-request.
