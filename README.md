# ECM

Bộ khởi tạo cho hệ thống ECM (Enterprise Content Management) được chia thành nhiều thành phần theo kiến trúc trong `ARCHITECT.md`. Repo hiện chứa skeleton để tiếp tục phát triển; chưa có phần cài đặt nghiệp vụ hay gói phụ thuộc cụ thể.

## Thư mục chính

```
/ECM.sln                # Solution gộp tất cả project .NET
/src
  ├── Aspire
  │   ├── ECM.AppHost         # Điểm khởi chạy Aspire (DistributedApplication)
  │   └── ECM.ServiceDefaults # Cấu hình chia sẻ cho mọi service .NET
  ├── app-gateway
  │   ├── AppGateway.Api/     # BFF + reverse proxy host (ASP.NET Core)
  │   ├── AppGateway.Contracts/
  │   ├── AppGateway.Infrastructure/
  │   └── ui/                 # SPA (React/Next/Vite) + build output
  ├── ecm
  │   ├── ECM.Host/           # Modular monolith host (nạp các module domain)
  │   ├── ECM.BuildingBlocks/ # Shared kernel, outbox, event abstractions
  │   └── Modules/            # Các module độc lập: Document, File, Workflow, ...
  ├── workers                # Nhóm background worker (OutboxDispatcher, SearchIndexer, Notify)
  ├── ocr
  │   ├── ocr-engine          # Service Python cho OCR
  │   └── labeling-ui         # UI gán nhãn dữ liệu OCR
  └── shared                  # Contracts, messaging, utilities, extensions dùng chung
/archive                # Lưu lại các microservice cũ để tham chiếu
/tests                  # Test project (xUnit) cho shared libraries
/docker                 # Tập tin phục vụ khởi tạo hạ tầng DEV
```

## Bắt đầu phát triển

1. Cài .NET 9 SDK.
2. Mở solution `ECM.sln` hoặc chạy build trực tiếp với `dotnet build ECM.sln`.
3. Chạy Aspire AppHost: `dotnet run --project src/Aspire/ECM.AppHost`.
4. Chạy test: `dotnet test ECM.sln` để xác nhận các cấu hình chung hoạt động.
