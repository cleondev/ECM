# ECM

Bộ khởi tạo cho hệ thống ECM (Enterprise Content Management) được chia thành nhiều thành phần theo kiến trúc trong [`ARCHITECT.md`](ARCHITECT.md). Repo đã được tổ chức lại đúng thiết kế, sẵn sàng mở rộng thêm nghiệp vụ.

## Mục lục

- [Cấu trúc thư mục chính](#cấu-trúc-thư-mục-chính)
- [Yêu cầu hệ thống](#yêu-cầu-hệ-thống)
- [Thiết lập hạ tầng phát triển](#thiết-lập-hạ-tầng-phát-triển)
  - [Cách 1: Dịch vụ đã cài trên server](#cách-1-dịch-vụ-đã-cài-trên-server)
  - [Cách 2: Docker Compose cho môi trường local](#cách-2-docker-compose-cho-môi-trường-local)
  - [Init nhanh biến môi trường](#init-nhanh-biến-môi-trường)
- [Khởi tạo cơ sở dữ liệu (EF Core migrations)](#khởi-tạo-cơ-sở-dữ-liệu-ef-core-migrations)
- [Làm việc với solution .NET](#làm-việc-với-solution-net)
- [SPA của App Gateway](#spa-của-app-gateway)
- [Kiểm thử](#kiểm-thử)
- [Tài liệu bổ sung](#tài-liệu-bổ-sung)

## Cấu trúc thư mục chính

```
/ECM.sln                # Solution gộp tất cả project .NET
/src
  ├── Aspire
  │   └── ECM.AppHost         # Điểm khởi chạy Aspire (DistributedApplication)
  ├── AppGateway
  │   ├── AppGateway.Api/     # BFF + reverse proxy host (ASP.NET Core)
  │   ├── AppGateway.Infrastructure/
  │   ├── AppGateway.Contracts/
  │   └── ui/                 # SPA (React/Next/Vite) + build output
  ├── ECM
  │   ├── ECM.Host/           # Modular monolith host (nạp các module domain)
  │   └── ECM.BuildingBlocks/ # Shared kernel, outbox, event abstractions
  ├── Modules/                # Các module độc lập: IAM, Document, File, Operations, Workflow, Signature, SearchRead, Ocr
  ├── Workers                 # Nhóm background worker (OutboxDispatcher, SearchIndexer, Notify, Ocr)
  ├── Ocr
  │   ├── ocr-engine          # Service Python cho OCR
  │   └── labeling-ui         # UI gán nhãn dữ liệu OCR
  └── Shared
      ├── Contracts/          # DTOs và message contract dùng chung
      ├── Extensions/         # Extension method chia sẻ
      ├── Messaging/          # Hạ tầng messaging
      ├── ServiceDefaults/    # Cấu hình mặc định cho mọi service .NET
      └── Utilities/          # Tiện ích dùng chung
/tests                  # Test project (xUnit) cho shared libraries
/deploy                 # Tập tin phục vụ khởi tạo hạ tầng DEV (Docker Compose, init scripts)
/docs                   # Tài liệu kiến trúc, API, quy trình
```

## Yêu cầu hệ thống

### Runtime

> ⚠️ **Quan trọng:** Hệ thống chạy trên .NET 9. Không được hạ cấp về .NET 8 vì sẽ làm hỏng toàn bộ quá trình build và chạy dịch vụ.

- **.NET SDK 9** – dùng để build và chạy toàn bộ service .NET.
- **Node.js ≥ 20** (kèm `npm`) – phục vụ phát triển/bundling SPA tại `src/AppGateway/ui` (Vite 5 yêu cầu Node 18 trở lên).
- **Python ≥ 3.11** – phục vụ các thành phần OCR (`src/Ocr`).

### Công cụ hỗ trợ

- **Docker & Docker Compose v2** – chạy hạ tầng PostgreSQL, MinIO và Redpanda trong môi trường DEV.
- **Git** – quản lý source.

## Thiết lập hạ tầng phát triển

Tùy bối cảnh mà lựa chọn chạy hạ tầng nền tảng trực tiếp trên server (DEV/Staging) hoặc bật nhanh bằng Docker Compose trên máy cá nhân.

### Cách 1: Dịch vụ đã cài trên server

1. **Cài đặt các dịch vụ bắt buộc**

   | Thành phần | Phiên bản khuyến nghị | Ghi chú cấu hình |
   |------------|-----------------------|------------------|
   | PostgreSQL | 16.x                  | Tạo database `ecm`, user `postgres` (hoặc user khác) với quyền owner. Kích hoạt `pg_trgm`, `uuid-ossp` và `citext` nếu chưa có. |
   | MinIO      | RELEASE.2024-03-30 trở lên | Tạo bucket `ecm-files`, tạo access key/secret dành riêng cho môi trường. |
   | Redpanda   | v23.3.x               | Khởi tạo topic phục vụ outbox/kafka theo script trong `deploy/init/topics-init.sh`. |

2. **Cấp thông tin kết nối cho ứng dụng**

   - Có thể cấu hình tại `src/ECM/ECM.Host/appsettings.json` hoặc thông qua biến môi trường.
   - Các biến cấu hình quan trọng:

    ```bash
    export ConnectionStrings__IAM="Host=<host>;Port=5432;Database=ecm_iam;Username=<db-user>;Password=<db-pass>"
    export ConnectionStrings__Document="Host=<host>;Port=5432;Database=ecm_doc;Username=<db-user>;Password=<db-pass>"
    export ConnectionStrings__File="Host=<host>;Port=5432;Database=ecm_doc;Username=<db-user>;Password=<db-pass>"
    export ConnectionStrings__Workflow="Host=<host>;Port=5432;Database=ecm_wf;Username=<db-user>;Password=<db-pass>"
    export ConnectionStrings__Search="Host=<host>;Port=5432;Database=ecm_search;Username=<db-user>;Password=<db-pass>"
    export ConnectionStrings__Ocr="Host=<host>;Port=5432;Database=ecm_ocr;Username=<db-user>;Password=<db-pass>"
    export ConnectionStrings__Operations="Host=<host>;Port=5432;Database=ecm_ops;Username=<db-user>;Password=<db-pass>"
    export FileStorage__ServiceUrl="http://<minio-host>:9000"
    export FileStorage__AccessKeyId=<minio-access-key>
    export FileStorage__SecretAccessKey=<minio-secret>
    export Kafka__BootstrapServers=<redpanda-host>:9092
    ```

   - Với nhiều môi trường, nên sử dụng `dotnet user-secrets` hoặc trình quản lý secrets tương ứng thay vì commit trực tiếp.

3. **Mở firewall & test kết nối** từ máy chạy ứng dụng (`dotnet run` hoặc worker) tới các dịch vụ trên server.

### Cách 2: Docker Compose cho môi trường local

1. Đảm bảo Docker Desktop/Engine và Docker Compose v2 hoạt động.
2. Khởi động các dịch vụ nền tảng (PostgreSQL, MinIO, Redpanda):

   ```bash
   docker compose -f deploy/compose.yml up -d
   ```

   - PostgreSQL: `localhost:5432`, user/password/database mặc định `ecm` (có thể đổi thông qua biến môi trường trong file compose).
   - MinIO: API `http://localhost:9000`, console `http://localhost:9001` (user `minio`, password `miniominio`).
   - Redpanda: broker `localhost:9092`, console `http://localhost:9644`.

3. Theo dõi log khi cần:

   ```bash
   docker compose -f deploy/compose.yml logs -f
   ```

4. Dừng toàn bộ hạ tầng khi không sử dụng:

   ```bash
   docker compose -f deploy/compose.yml down
   ```

Các script khởi tạo (schema DB mẫu, bucket/object, topic) nằm trong `deploy/init` và được docker compose tự động chạy.

- `deploy/init/topics-init.sh`: Tạo các topic chuẩn cho hệ thống (`iam.events`, `document.events`, `version.events`, `workflow.events`, `signature.events`, `ocr.events`, `search.events`, `audit.events`, `retention.events`). Có thể override broker/partition thông qua các biến môi trường `KAFKA_BROKER`, `KAFKA_TOPIC_PARTITIONS`, `KAFKA_TOPIC_REPLICAS` khi cần.

> **Lưu ý:** Nếu muốn tái tạo dữ liệu sạch, hãy xóa các volume `pgdata`, `minio-data` trước khi `up` trở lại.

### Init nhanh biến môi trường

Để không phải gõ lại từng biến cấu hình, repo cung cấp sẵn script `deploy/scripts/init-all.sh` (Bash) và `deploy/scripts/init-all.ps1` (PowerShell). Các script này dựa trên thông số mặc định của `deploy/compose.yml` (PostgreSQL user/password `ecm`, MinIO `minio/miniominio`, Redpanda `localhost:9092`).

- **macOS/Linux (Bash/zsh):**

  ```bash
  source deploy/scripts/init-all.sh
  ```

- **Windows (PowerShell):**

  ```powershell
  .\deploy\scripts\init-all.ps1
  ```

Sau khi chạy, các biến sau sẽ được thiết lập: `ConnectionStrings__IAM`, `ConnectionStrings__Document`, `ConnectionStrings__File`, `ConnectionStrings__Workflow`, `ConnectionStrings__Search`, `ConnectionStrings__Ocr`, `ConnectionStrings__Operations`, `FileStorage__*`, `Kafka__BootstrapServers`, `Services__Ecm`, `Workflow__Camunda__BaseUrl`, `Workflow__Camunda__TenantId`. Có thể tùy chỉnh trước bằng cách đặt các biến `DB_HOST`, `DB_NAME_PREFIX`, `DB_USER`, `FileStorage__ServiceUrl`,... rồi mới `source`/chạy script.

## Khởi tạo cơ sở dữ liệu (EF Core migrations)

Các module sử dụng Entity Framework Core để quản lý schema. Bộ khởi tạo hiện bao gồm module Document với migrations có sẵn tại `src/Modules/Document/Infrastructure/Migrations`.

1. **Cài công cụ `dotnet-ef`** (cùng major version 9.x với EF Core trong solution):

   ```bash
   dotnet tool install --global dotnet-ef --version 9.0.10
   # Nếu đã cài, có thể cập nhật: dotnet tool update --global dotnet-ef --version 9.0.10
   ```

2. **Đảm bảo kết nối cơ sở dữ liệu** hoạt động (theo một trong hai cách ở trên) và cấu hình biến môi trường `ConnectionStrings__Document`. Ví dụ với môi trường local chạy docker compose:

   ```bash
   export ConnectionStrings__Document="Host=localhost;Port=5432;Database=ecm_doc;Username=ecm;Password=ecm"
   ```

   Trên PowerShell (Windows):

   ```powershell
   $Env:ConnectionStrings__Document = "Host=localhost;Port=5432;Database=ecm_doc;Username=ecm;Password=ecm"
   ```

   > **Mẹo:** với môi trường server, thay `localhost` bằng địa chỉ thực tế và thông tin user/password tương ứng.
   > **Lưu ý:** `Database` (ví dụ `ecm_doc`) và `Username` (ví dụ `ecm`) là hai tham số khác nhau của connection string — đừng hoán đổi chúng khi cấu hình.
   > **Ghi chú:** file `appsettings.json` mẫu trong `ECM.Host` dùng user `postgres`. Nếu chạy Docker Compose với user `ecm`, hãy override bằng biến môi trường như trên hoặc chỉnh sửa file cấu hình cho trùng khớp.

3. **Chạy migrate để khởi tạo schema** (từ thư mục gốc repo):

   ```bash
   dotnet ef database update \
     --project src/Modules/Document/ECM.Document.csproj \
     --startup-project src/ECM/ECM.Host/ECM.Host.csproj \
     --context ECM.Document.Infrastructure.Persistence.DocumentDbContext
   ```

   Lệnh trên sử dụng `ECM.Host` làm startup project để nạp cấu hình và dependency injection. Có thể truyền thêm `-- --environment Development` nếu cần ép môi trường cụ thể.

4. **Xác nhận kết quả**

   - Kiểm tra bảng đã được tạo trong database `ecm` (`\dt doc.*` trong psql).
   - Nếu thay đổi migrations hoặc thêm module mới, lặp lại bước 3 với context tương ứng.

Trong trường hợp cần seed dữ liệu mẫu hoặc tạo topic/bucket, tham khảo thêm các script trong `deploy/init`.

## Làm việc với solution .NET

### Khôi phục và build

```bash
dotnet restore ECM.sln
# Build chế độ Debug (mặc định)
dotnet build ECM.sln
```

### Chạy toàn bộ hệ thống bằng Aspire

Aspire AppHost giúp orchestration các project .NET và kết nối tới hạ tầng Docker.

```bash
dotnet run --project src/Aspire/ECM.AppHost
```

Các project sẽ được khởi chạy kèm cấu hình connection string từ AppHost (`app-ecm` monolith, `svc-app-gateway`, các worker tiền tố `worker-`...). Aspire Dashboard mặc định trên `http://localhost:18888`.

Khi mở Aspire Dashboard, bạn sẽ nhìn thấy các resource của hệ thống cùng URL đã được AppHost gán sẵn:

| Ứng dụng (resource) | Chức năng | URL từ Aspire Dashboard |
|---------------------|-----------|-------------------------|
| `Aspire Dashboard`  | Quan sát trạng thái toàn bộ ứng dụng Aspire | `http://localhost:18888` |
| `app-ecm`           | Monolith ECM.Host chạy nghiệp vụ chính | `http://localhost:8080` |
| `svc-app-gateway`   | BFF + reverse proxy phục vụ UI | `http://localhost:5090` |
| `worker-search-indexer`    | Worker cập nhật chỉ mục tìm kiếm | – (background worker, không expose HTTP) |
| `worker-ocr`        | Worker gọi Dot OCR service | – (background worker, không expose HTTP) |
| `worker-outbox-dispatcher` | Worker đọc outbox và đẩy sự kiện ra Kafka | – (background worker, không expose HTTP) |
| `worker-notify`     | Worker gửi thông báo (email/webhook) | – (background worker, không expose HTTP) |

### Chạy thủ công từng service

- Monolith ECM:

  ```bash
  dotnet run --project src/ECM/ECM.Host/ECM.Host.csproj
  ```

- App Gateway (BFF + reverse proxy):

  ```bash
  dotnet run --project src/AppGateway/AppGateway.Api/AppGateway.Api.csproj
  ```

  Đảm bảo biến cấu hình `Services__Ecm` trỏ tới địa chỉ của monolith (ví dụ `http://localhost:8080`). Có thể đặt trong `appsettings.Development.json` hoặc thông qua biến môi trường.

- Background workers (ví dụ Outbox Dispatcher):

  ```bash
  dotnet run --project src/Workers/OutboxDispatcher.Worker/OutboxDispatcher.Worker.csproj
  ```

  Lặp lại tương tự cho các worker khác trong thư mục `src/Workers`.

### Cấu hình worker OutboxDispatcher, SearchIndexer & Ocr

Các background worker không đọc cấu hình từ `appsettings` chung của monolith mà mong đợi biến môi trường tương ứng khi chạy độc
lập (hoặc thông qua Aspire AppHost). Một số thiết lập quan trọng:

- **OutboxDispatcher** cần kết nối PostgreSQL để đọc bảng `ops.outbox`. Worker sử dụng `ConnectionStrings__Operations`, hãy đảm
  bảo biến này trỏ tới đúng database/schema:

  ```bash
  export ConnectionStrings__Operations="Host=localhost;Port=5432;Database=ecm_ops;Username=ecm;Password=ecm"
  ```

  > PowerShell:
  >
  > ```powershell
  > $Env:ConnectionStrings__Operations = "Host=localhost;Port=5432;Database=ecm_ops;Username=ecm;Password=ecm"
  > ```

- **SearchIndexer** nghe các sự kiện từ Kafka/Redpanda. Cấu hình được bind vào section `Kafka` của worker, tương ứng với các biến môi trường `Kafka__*`. Tối thiểu cần thiết lập `BootstrapServers`; các tham số khác (group id, client id, offset...) có thể để mặc định hoặc override khi cần:

  ```bash
  export Kafka__BootstrapServers=localhost:9092
  # tuỳ chọn:
  export Kafka__GroupId=search-indexer
  export Kafka__EnableAutoCommit=true
  export Kafka__AutoOffsetReset=Earliest
  ```

  > PowerShell:
  >
  > ```powershell
  > $Env:Kafka__BootstrapServers = "localhost:9092"
  > ```

- **Ocr.Worker** cũng sử dụng Kafka để lắng nghe `ecm.document.uploaded` và gọi Dot OCR service. Ngoài cấu hình Kafka giống bên trên, cần cấp URL dịch vụ thông qua `Ocr__Dot__BaseUrl` (và các tham số tuỳ chọn như `Ocr__Dot__ApiKey`, `Ocr__Dot__TimeoutSeconds` nếu cần):

  ```bash
  export Kafka__BootstrapServers=localhost:9092
  export Ocr__Dot__BaseUrl=http://localhost:7075/
  # tuỳ chọn
  export Ocr__Dot__ApiKey=<token>
  ```

  > PowerShell:
  >
  > ```powershell
  > $Env:Kafka__BootstrapServers = "localhost:9092"
  > $Env:Ocr__Dot__BaseUrl = "http://localhost:7075/"
  > ```

Aspire AppHost giúp orchestration các project .NET và kết nối tới hạ tầng Docker.

```bash
dotnet run --project src/Aspire/ECM.AppHost
```

Các project sẽ được khởi chạy kèm cấu hình connection string từ AppHost (`app-ecm` monolith, `svc-app-gateway`, các worker tiền tố `worker-`...). Aspire Dashboard mặc định trên `http://localhost:18888`.

Khi mở Aspire Dashboard, bạn sẽ nhìn thấy các resource của hệ thống cùng URL đã được AppHost gán sẵn:

| Ứng dụng (resource) | Chức năng | URL từ Aspire Dashboard |
|---------------------|-----------|-------------------------|
| `Aspire Dashboard`  | Quan sát trạng thái toàn bộ ứng dụng Aspire | `http://localhost:18888` |
| `app-ecm`           | Monolith ECM.Host chạy nghiệp vụ chính | `http://localhost:8080` |
| `svc-app-gateway`   | BFF + reverse proxy phục vụ UI | `http://localhost:5090` |
| `worker-search-indexer`    | Worker cập nhật chỉ mục tìm kiếm | – (background worker, không expose HTTP) |
| `worker-ocr`        | Worker gọi Dot OCR service | – (background worker, không expose HTTP) |
| `worker-outbox-dispatcher` | Worker đọc outbox và đẩy sự kiện ra Kafka | – (background worker, không expose HTTP) |
| `worker-notify`     | Worker gửi thông báo (email/webhook) | – (background worker, không expose HTTP) |

### Chạy thủ công từng service

- Monolith ECM:

  ```bash
  dotnet run --project src/ECM/ECM.Host/ECM.Host.csproj
  ```

- App Gateway (BFF + reverse proxy):

  ```bash
  dotnet run --project src/AppGateway/AppGateway.Api/AppGateway.Api.csproj
  ```

  Đảm bảo biến cấu hình `Services__Ecm` trỏ tới địa chỉ của monolith (ví dụ `http://localhost:8080`). Có thể đặt trong `appsettings.Development.json` hoặc thông qua biến môi trường.

- Background workers (ví dụ Outbox Dispatcher):

  ```bash
  dotnet run --project src/Workers/OutboxDispatcher.Worker/OutboxDispatcher.Worker.csproj
  ```

  Lặp lại tương tự cho các worker khác trong thư mục `src/Workers`.

### Cấu hình worker OutboxDispatcher, SearchIndexer & Ocr

Các background worker không đọc cấu hình từ `appsettings` chung của monolith mà mong đợi biến môi trường tương ứng khi chạy độc
lập (hoặc thông qua Aspire AppHost). Một số thiết lập quan trọng:

- **OutboxDispatcher** cần kết nối PostgreSQL để đọc bảng `ops.outbox`. Worker sử dụng `ConnectionStrings__Operations`, hãy đảm
  bảo biến này trỏ tới đúng database/schema:

  ```bash
  export ConnectionStrings__Operations="Host=localhost;Port=5432;Database=ecm_ops;Username=ecm;Password=ecm"
  ```

  > PowerShell:
  >
  > ```powershell
  > $Env:ConnectionStrings__Operations = "Host=localhost;Port=5432;Database=ecm_ops;Username=ecm;Password=ecm"
  > ```

- **SearchIndexer** nghe các sự kiện từ Kafka/Redpanda. Cấu hình được bind vào section `Kafka` của worker, tương ứng với các biến
  môi trường `Kafka__*`. Tối thiểu cần thiết lập `BootstrapServers`; các tham số khác (group id, client id, offset...) có thể để
  mặc định hoặc override khi cần:

  ```bash
  export Kafka__BootstrapServers=localhost:9092
  # tuỳ chọn:
  export Kafka__GroupId=search-indexer
  export Kafka__EnableAutoCommit=true
  export Kafka__AutoOffsetReset=Earliest
  ```

  > PowerShell:
  >
  > ```powershell
  > $Env:Kafka__BootstrapServers = "localhost:9092"
  > ```

Aspire AppHost (`dotnet run --project src/Aspire/ECM.AppHost`) đã điền sẵn các giá trị từ file `src/Aspire/ECM.AppHost/appsettings.json`
và `appsettings.Development.json`. Khi chạy thủ công ngoài Aspire, cần tự cấp biến môi trường như ví dụ trên để worker hoạt động
đúng.

## SPA của App Gateway

Frontend nằm trong `src/AppGateway/ui` (Vite). Các bước cơ bản:

```bash
cd src/AppGateway/ui
npm install          # cài đặt phụ thuộc
npm run dev -- --host  # chạy chế độ phát triển, expose ra mạng LAN nếu cần
```

Build và preview production bundle:

```bash
npm run build
npm run preview -- --host
```

Thành phẩm build nằm ở `src/AppGateway/ui/dist` và được App Gateway phục vụ khi deploy.

## Kiểm thử

Toàn bộ test .NET được gom trong solution `ECM.sln`:

```bash
dotnet test ECM.sln
```

Có thể chạy đơn lẻ từng project hoặc filter:

```bash
# Chạy riêng test của module Document
dotnet test tests/Document.Tests/Document.Tests.csproj

# Lọc theo namespace/test name
dotnet test ECM.sln --filter FullyQualifiedName~Document
```

## Tài liệu bổ sung

- [ARCHITECT.md](ARCHITECT.md) – mô tả kiến trúc tổng thể và các nguyên tắc thiết kế.
- [docs/README.md](docs/README.md) – điểm bắt đầu để khám phá tài liệu chi tiết hơn.
- [docs/ocr-integration.md](docs/ocr-integration.md) – hướng dẫn tích hợp Dot OCR (module + worker).
- [docs/environment-configuration.md](docs/environment-configuration.md) – hướng dẫn ánh xạ biến môi trường, Azure secrets và thiết lập DEV local.
