# ARCHITECT.md

> DMS – Document Management System  
> **Stack:** .NET 8 (services), Python (OCR), PostgreSQL 16, MinIO, Redpanda (Kafka-compatible), Docker Desktop Linux (DEV)  
> **Style:** Modular-monolith first, split-by-seams later. Event-driven via Outbox → Broker.

---

## 1) System Overview

```
[ecm]
   ├─▶ [document-services] ──┬─▶ (file-services ⇄ MinIO)
   │                              └─▶ (bus.outbox → Redpanda topics)
   ├─▶ [workflow]
   ├─▶ [form]
   └─▶ [ocr (Python)]

Consumers:
  - search-indexer  (consume events → build FTS/KV/Vector)
  - notify          (consume events → email/webhook)
  - audit           (consume events → append audit)
  - retention       (jobs + consume retention.*)
  - outbox-dispatcher (poll DB → publish to Redpanda)
```

**Principles**

- Clear bounded contexts: `document`, `workflow`, `ocr`, `search`, `signature`, `retention`, `audit`.
- Single DB (PostgreSQL schemas) for DEV; RLS for read-authorization.
- Outbox pattern for reliable eventing; consumers idempotent (UPSERT).
- Hybrid Search: FTS (tsvector) + Vector (pgvector) + KV filter.
- OCR is **Python** (Tesseract/PaddleOCR) + labeling UI; promote to metadata via API only.

---


## 2) Aspire Integration

**Purpose:** sử dụng .NET Aspire để điều phối, giám sát và cấu hình nhiều service dễ dàng khi dev.

### 🧩 Cấu trúc Aspire

```
repo/
├─ host/
│  └─ AppHost/            # Aspire Orchestrator (root)
├─ libs/
│  └─ ServiceDefaults/    # Telemetry, Resilience, Health defaults
└─ apps/
   ├─ ecm/                # Gateway (edge)
   ├─ document-services/  # CRUD + Outbox
   ├─ search-indexer/     # Consumer worker
   ├─ outbox-dispatcher/  # Producer worker
   ├─ workflow/           # Camunda client
   └─ ocr/ (Python)
```

### ⚙️ AppHost cấu hình

```csharp
var builder = DistributedApplication.CreateBuilder(args);
var pg = builder.AddConnectionString("postgres");
var kafka = builder.AddConnectionString("kafka");
var minio = builder.AddConnectionString("minio");

var document = builder.AddProject<Projects.DocumentServices>("document-services")
    .WithReference(pg).WithReference(kafka).WithReference(minio);

builder.AddProject<Projects.SearchIndexer>("search-indexer").WithReference(pg).WithReference(kafka);
builder.AddProject<Projects.OutboxDispatcher>("outbox-dispatcher").WithReference(pg).WithReference(kafka);

builder.Build().Run();
```

### 🧠 ServiceDefaults

Thư viện `ServiceDefaults` chứa cấu hình mặc định cho mọi service:
- **OpenTelemetry** (Metrics, Tracing)
- **HealthChecks** (liveness / readiness)
- **HttpClient Resilience** (`AddStandardResilienceHandler`)

### 🔍 Dev Flow

- Chạy Aspire Dashboard: `dotnet run --project host/AppHost`.
- Theo dõi health / logs / dependencies trực quan.
- Có thể trỏ Aspire tới external Postgres/Redpanda (đã chạy bằng Docker Compose ngoài).

**Lợi ích:**
- Giảm cấu hình Docker Compose phức tạp khi dev nhiều service.
- Quản lý môi trường (connection strings, telemetry, secrets) tập trung.
- Dễ mở rộng ra Observability (Grafana, OTLP, Prometheus) sau này.

---

## 2) Projects & Modules

| Project               | Lang       | Purpose                           | Key Modules                                                     |
| --------------------- | ---------- | --------------------------------- | --------------------------------------------------------------- |
| **ecm**               | .NET 8     | Edge auth + routing               | main application                                                |
| **document-services** | .NET 8     | Documents + Versions + Outbox     | documents, versions, metadata-merge, outbox                     |
| **file-services**     | .NET 8     | Presigned URLs + MinIO adapter    | presign, lifecycle                                              |
| **workflow**          | .NET 8     | Workflow definition/instance/task | wf-definition, wf-instance, wf-task                             |
| **ocr**               | **Python** | OCR + labeling + extraction       | ocr-engine, template-registry, labeling-ui, extraction-resolver |
| **search-api**        | .NET 8     | Search read API (hybrid)          | , ranker                                                        |
| **search-indexer**    | .NET 8     | Build indexes from events         | indexer, fts-writer, kv-writer, vector-writer                   |
| **notify**            | .NET 8     | Email/Webhook                     | subscriptions, delivery, templating                             |
| **signature**         | .NET 8     | Sign/Verify adapters              | sign-adapter, verify, evidence-store                            |
| **outbox-dispatcher** | .NET 8     | Poll DB → publish events          | poller, publisher, dlq                                          |

**Shared packages** (avoid god-library):

- `shared-interfaces` (IStorage, IAuthzContext, Result, Id types)
- `shared-utils` (hashing/time/json)
- `storage-minio-adapter` (S3 SDK)
- `authz-postgres-rls-adapter` (SET LOCAL app.user\_\*)
- `search-helpers` (cosine/normalize & DTO)
- `search-index-contracts` (event payloads)

---

## 3) Data Model (DB) – Summary

**Schemas (DEV, single-tenant simplified):** `iam, authz, doc, files, bus, ocr, wf, search, audit, retention, signature`

- `doc.document(id, title, doc_type, status, sensitivity, owner_id, created_at, updated_at)`
- `doc.version(id, document_id, version_no, storage_key, sha256, bytes, mime_type, created_by, created_at)`
- `doc.metadata(document_id, data jsonb)` (GIN)

- `files.object(storage_key, legal_hold)` (MinIO key)

- `bus.outbox(id, aggregate, aggregate_id, type, payload, processed_at)` + `bus.outbox_deadletter(...)`

- OCR:

  - `ocr.result(document_id, version_id, pages, lang, summary)`
  - `ocr.page_text(document_id, version_id, page_no, content)`
  - `ocr.template(id, name, version, page_side, size_ratio, is_active)`
  - `ocr.field_def(id, template_id, field_key, bbox_rel, anchor, validator, required, order_no)`
  - `ocr.annotation(id, document_id, version_id, template_id, field_key, value_text, bbox_abs, confidence, source, created_by)`
  - `ocr.extraction(document_id, version_id, field_key, value_text, confidence, provenance)`
  - `ocr.label_project / ocr.label_task` (optional labeling workflow)

- Workflow:

  - `wf.definition(id, name, spec jsonb)`
  - `wf.instance(id, definition_id, document_id, state, created_at, updated_at)`
  - `wf.task(id, instance_id, step_id, assignee_id, due_at, status, acted_at)`

- Search:

  - `search.fts(document_id, tsv)` (GIN)
  - `search.kv(document_id, field_key, field_value)` (+ trigram)
  - `search.embedding(document_id, title_vec(768), text_vec(1536))` (ivfflat cosine)

- Audit (append-only): `audit.event(id, occurred_at, actor_id, action, object_type, object_id, details)`

- Retention: `retention.policy`, `retention.candidate`

- Signature: `signature.result(document_id, version_id, status, evidence, ...)`

**RLS (read)** on `doc.document` (+ join applies):  
`authz.fn_can_read(doc_row)` combines RBAC (user_role), ReBAC (relationship), and ABAC (metadata.department).

---

## 4) Events & Outbox

**Outbox types → Topics**

| Type (prefix)             | Topic              |
| ------------------------- | ------------------ |
| `document.*`, `version.*` | `document.events`  |
| `workflow.*`              | `workflow.events`  |
| `ocr.*`                   | `ocr.events`       |
| `search.*`                | `search.events`    |
| `retention.*`             | `retention.events` |
| `signature.*`             | `signature.events` |

**Dispatcher**: `outbox-dispatcher` polls `bus.outbox` in batches (`FOR UPDATE SKIP LOCKED`) → publish → set `processed_at`. DLQ to `bus.outbox_deadletter` for permanent failures.

**Idempotency**: consumers must `UPSERT` (e.g., `ON CONFLICT` for search indexes).

---

## 5) APIs (samples)

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

- `GET /search?q=&mode=hybrid&filters=department:Credit,doc_type:Contract`
  - Combines FTS rank + cosine similarity from `search.embedding` + KV filters.

### OCR

- `POST /ocr/process` (optional trigger)
- Labeling UI endpoints (annotation CRUD)
- Resolver promotes `ocr.extraction` and emits `ocr.extraction.updated`

---

## 6) Directory Layout

```
repo/
├─ host/
│  └─ AppHost/                        # .NET Aspire orchestrator
├─ apps/
│  ├─ ecm/
│  │  └─ Ecm.Api/                     # Gateway/BFF + serve UI tĩnh (wwwroot)
│  │     └─ wwwroot/                  # Build SPA (copy vào đây)
│  ├─ document/                       # Bounded Context: Document (Clean Architecture)
│  │  ├─ Document.Api/
│  │  ├─ Document.Application/
│  │  ├─ Document.Domain/
│  │  └─ Document.Infrastructure/
│  ├─ workflow/                       # Bounded Context: Workflow (Clean Architecture)
│  │  ├─ Workflow.Api/
│  │  ├─ Workflow.Application/
│  │  ├─ Workflow.Domain/
│  │  └─ Workflow.Infrastructure/
│  ├─ search/
│  │  └─ Search.Indexer/              # Worker build index từ events
│  └─ background/
│     └─ OutboxDispatcher.Worker/     # Poll DB outbox → publish Redpanda
├─ libs/
│   ├─ ServiceDefaults/                # OTel, health, resilience defaults cho mọi host
│   ├─ SharedKernel/                   # Abstractions trung tính domain (Result, Id, etc.)
│   └─ Contracts/                      # Event/DTO contracts dùng xuyên context
└─ docker/
   ├─ compose.yml
   └─ init/
       ├─ db-init.sql
       ├─ mc-init.sh
       └─ topics-init.sh
```

---

## 7) Docker (DEV) – Summary

**Infra services**

- **Postgres 16** (`5432`): enable `citext`, `pg_trgm`, `vector`
- **MinIO** (`9000`, console `9001`): bucket `${MINIO_BUCKET}`
- **Redpanda** (`9092`, admin `9644`) + console (`8089`)
- **Mailhog** (`8025`) optional
- **Wiremock** (`8088`) optional

**Applications**

- `gateway` (`8080`)
- `document-services` (`8081`)
- `file-services` (`8082`)
- `workflow` (`8083`)
- `ocr` (Python) (`8084`)
- `search-api` (`8085`)
- Workers: `search-indexer`, `notify`, `audit`, `retention`, `outbox-dispatcher`

**Key ENV**

```
POSTGRES_USER, POSTGRES_PASSWORD, POSTGRES_DB
MINIO_ROOT_USER, MINIO_ROOT_PASSWORD, MINIO_BUCKET
KAFKA_BROKER=redpanda:9092
JWT_SECRET=dev-secret
TESSDATA_LANG=vie+eng
```

---

## 8) Build & Run (DEV)

```bash
# 1) Prepare
cp .env.example .env   # fill values
# 2) Start infra + apps
docker compose -f docker/compose.yml up -d --build
# 3) Check UIs
open http://localhost:9001   # MinIO console
open http://localhost:5050   # pgAdmin (if enabled)
open http://localhost:8089   # Redpanda console
open http://localhost:8080   # Gateway
```

**DB init**: put full DDL into `docker/init/db-init.sql` (RLS + schemas).  
**Outbox**: dispatcher will publish events to topics; consumers update read models.

---

## 9) Security & RLS

- API layer sets:  
  `SET LOCAL app.user_id = '<uuid>'; SET LOCAL app.user_department = '<text>';`
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
- OCR (Python) interacts via REST/events; store embeddings via indexer (not directly from OCR).
