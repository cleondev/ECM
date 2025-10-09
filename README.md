# ECM

Bộ khởi tạo cho hệ thống ECM (Enterprise Content Management) được chia thành nhiều service nhỏ theo kiến trúc trong `ARCHITECT.md`. Kho repo hiện chỉ chứa các skeleton để bắt đầu phát triển; chưa có phần cài đặt nghiệp vụ hay gói phụ thuộc cụ thể.

## Thư mục chính

```
/ECM.sln                # Solution gộp tất cả project .NET
/src
  ├── Aspire
  │   ├── ECM.AppHost         # Điểm khởi chạy Aspire (DistributedApplication)
  │   └── ECM.ServiceDefaults # Cấu hình chia sẻ cho mọi service .NET
  ├── app-gateway             # Gateway + UI (placeholder)
  ├── ecm
  │   └── ECM.Host            # Modular monolith (nạp các module domain)
  ├── workers                # Nhóm background worker (Outbox, SearchIndexer, ...)
  ├── ocr
  │   ├── ocr-engine          # Service Python cho OCR
  │   └── labeling-ui         # Placeholder cho UI gán nhãn
  └── shared                  # Hạ tầng chia sẻ (contracts, messaging, ...)
/archive                # Lưu lại các microservice cũ để tham chiếu
/tests                  # Test project (xUnit) cho shared libraries
/docker                 # Tập tin phục vụ khởi tạo hạ tầng DEV
```

## Bắt đầu phát triển

1. Cài .NET 9 SDK
2. Mở solution `ECM.sln` hoặc chạy build trực tiếp với `dotnet build ECM.sln`.
3. Chạy Aspire AppHost: `dotnet run --project src/Aspire/ECM.AppHost`.
4. Chạy test: `dotnet test ECM.sln` để xác nhận các cấu hình chung hoạt động.