# ECM

Bộ khởi tạo cho hệ thống ECM (Enterprise Content Management) được chia thành nhiều service nhỏ theo kiến trúc trong `ARCHITECT.md`. Kho repo hiện chỉ chứa các skeleton để bắt đầu phát triển; chưa có phần cài đặt nghiệp vụ hay gói phụ thuộc cụ thể.

## Thư mục chính

```
/ECM.sln                # Solution gộp tất cả project .NET
/host/AppHost           # Điểm khởi chạy Aspire (DistributedApplication)
/libs/ServiceDefaults   # Cấu hình chia sẻ cho mọi service .NET
/apps                   # Tập hợp các ứng dụng và worker
  ├── apis              # Nhóm API/HTTP service
  │   ├── ecm               # API gateway/edge
  │   ├── document-services # CRUD tài liệu + outbox
  │   ├── file-services     # Presign MinIO
  │   └── search-api        # API tra cứu
  ├── workers           # Nhóm background worker
  │   ├── workflow          # Quản lý workflow
  │   ├── search-indexer    # Worker build search index
  │   ├── outbox-dispatcher # Worker phát sự kiện
  │   ├── notify            # Worker gửi thông báo
  │   ├── audit             # Worker lưu vết
  │   └── retention         # Worker thực thi chính sách lưu trữ
  └── python            # Service không dùng .NET
      └── ocr               # Service Python cho OCR
/tests                  # Test project (xUnit) cho shared libraries
/docker                 # Tập tin phục vụ khởi tạo hạ tầng DEV
```

## Bắt đầu phát triển

1. Cài .NET 9 SDK
2. Mở solution `ECM.sln` hoặc chạy build trực tiếp với `dotnet build ECM.sln`.
3. Chạy Aspire AppHost: `dotnet run --project host/AppHost`.
4. Chạy test: `dotnet test ECM.sln` để xác nhận các cấu hình chung hoạt động.