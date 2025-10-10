# ECM

Bộ khởi tạo cho hệ thống ECM (Enterprise Content Management) được chia thành nhiều thành phần theo kiến trúc trong `ARCHITECT.md`. Repo đã được tổ chức lại đúng thiết kế, sẵn sàng mở rộng thêm nghiệp vụ.

## Thư mục chính

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

## Bắt đầu phát triển

1. Cài .NET 9 SDK.
2. Mở solution `ECM.sln` hoặc chạy build trực tiếp với `dotnet build ECM.sln`.
3. Chạy Aspire AppHost: `dotnet run --project src/Aspire/ECM.AppHost`.
4. Chạy test: `dotnet test ECM.sln` để xác nhận các cấu hình chung hoạt động.
