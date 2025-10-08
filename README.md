# ECM

Bộ khởi tạo cho hệ thống ECM (Enterprise Content Management) được chia thành nhiều service nhỏ theo kiến trúc trong `ARCHITECT.md`. Kho repo hiện chỉ chứa các skeleton để bắt đầu phát triển; chưa có phần cài đặt nghiệp vụ hay gói phụ thuộc cụ thể.

## Thư mục chính

```
/host/AppHost           # Điểm khởi chạy Aspire (placeholder)
/libs/ServiceDefaults   # Cấu hình chia sẻ cho mọi service .NET
/apps                   # Tập hợp các ứng dụng và worker
  ├── ecm               # API gateway/edge
  ├── document-services # CRUD tài liệu + outbox
  ├── file-services     # Presign MinIO
  ├── workflow          # Quản lý workflow
  ├── search-api        # API tra cứu
  ├── search-indexer    # Worker build search index
  ├── outbox-dispatcher # Worker phát sự kiện
  ├── notify            # Worker gửi thông báo
  ├── audit             # Worker lưu vết
  ├── retention         # Worker thực thi chính sách lưu trữ
  └── ocr               # Service Python cho OCR
/docker                 # Tập tin phục vụ khởi tạo hạ tầng DEV
```

## Bắt đầu phát triển

1. Cài .NET 8 SDK và Python 3.11 trở lên.
2. Tạo solution và thêm các project theo nhu cầu (`dotnet new sln && dotnet sln add ...`).
3. Bổ sung gói NuGet/AspNetCore, thư viện Aspire khi cần.
4. Đối với OCR, tạo virtualenv và cài đặt `pip install -e .` trong `apps/ocr`.
5. Cập nhật `docker/compose.yml` để mô phỏng hạ tầng Postgres, MinIO, Redpanda theo kiến trúc.

Các file mã nguồn hiện chủ yếu nhằm mô tả contract, giữ cho dự án biên dịch được khi bổ sung SDK tương ứng.
