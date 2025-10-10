# ECM

Bộ khởi tạo cho hệ thống ECM (Enterprise Content Management) được chia thành nhiều thành phần theo kiến trúc trong [`ARCHITECT.md`](ARCHITECT.md). Repo đã được tổ chức lại đúng thiết kế, sẵn sàng mở rộng thêm nghiệp vụ.

## Mục lục

- [Cấu trúc thư mục chính](#cấu-trúc-thư-mục-chính)
- [Yêu cầu hệ thống](#yêu-cầu-hệ-thống)
- [Thiết lập hạ tầng phát triển](#thiết-lập-hạ-tầng-phát-triển)
- [Làm việc với solution .NET](#làm-việc-với-solution-net)
- [SPA của App Gateway](#spa-của-app-gateway)
- [Kiểm thử](#kiểm-thử)
- [Tài liệu bổ sung](#tài-liệu-bổ-sung)

## Cấu trúc thư mục chính

```
/ECM.sln                # Solution gộp tất cả project .NET
/src
  ├── Aspire
  │   ├── ECM.AppHost         # Điểm khởi chạy Aspire (DistributedApplication)
  │   └── ECM.ServiceDefaults # Cấu hình chia sẻ cho mọi service .NET
  ├── AppGateway
  │   ├── AppGateway.Api/     # BFF + reverse proxy host (ASP.NET Core)
  │   ├── AppGateway.Infrastructure/
  │   ├── AppGateway.Contracts/
  │   └── ui/                 # SPA (React/Next/Vite) + build output
  ├── ECM
  │   ├── ECM.Host/           # Modular monolith host (nạp các module domain)
  │   └── ECM.BuildingBlocks/ # Shared kernel, outbox, event abstractions
  ├── Modules/                # Các module độc lập: Document, File, Workflow, Signature, SearchRead
  ├── Workers                 # Nhóm background worker (OutboxDispatcher, SearchIndexer, Notify)
  ├── Ocr
  │   ├── ocr-engine          # Service Python cho OCR
  │   └── labeling-ui         # UI gán nhãn dữ liệu OCR
  └── Shared                  # Contracts, messaging, utilities, extensions dùng chung
/tests                  # Test project (xUnit) cho shared libraries
/deploy                 # Tập tin phục vụ khởi tạo hạ tầng DEV (Docker Compose, init scripts)
/docs                   # Tài liệu kiến trúc, API, quy trình
```

## Yêu cầu hệ thống

### Runtime

- **.NET SDK 9** – dùng để build và chạy toàn bộ service .NET.
- **Node.js ≥ 20** (kèm `npm`) – phục vụ phát triển/bundling SPA tại `src/AppGateway/ui` (Vite 5 yêu cầu Node 18 trở lên).
- **Python ≥ 3.11** – phục vụ các thành phần OCR (`src/Ocr`).

### Công cụ hỗ trợ

- **Docker & Docker Compose v2** – chạy hạ tầng PostgreSQL, MinIO và Redpanda trong môi trường DEV.
- **Git** – quản lý source.

## Thiết lập hạ tầng phát triển

1. Đảm bảo Docker đang hoạt động.
2. Khởi động các dịch vụ nền tảng (PostgreSQL, MinIO, Redpanda):

   ```bash
   docker compose -f deploy/compose.yml up -d
   ```

   - PostgreSQL: `localhost:5432`, user/password/database mặc định `ecm`.
   - MinIO: S3-compatible tại `http://localhost:9000`, console `http://localhost:9001` (user `minio`, password `miniominio`).
   - Redpanda: `localhost:9092`, console `http://localhost:9644`.

3. Theo dõi log khi cần: `docker compose -f deploy/compose.yml logs -f`.
4. Dừng toàn bộ hạ tầng khi không sử dụng:

   ```bash
   docker compose -f deploy/compose.yml down
   ```

Các script khởi tạo (schema DB, bucket/object, topic) nằm trong `deploy/init`.

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

Các project sẽ được khởi chạy kèm cấu hình connection string từ AppHost (`ecm` monolith, gateway, worker...). Aspire Dashboard mặc định trên `http://localhost:18888`.

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
