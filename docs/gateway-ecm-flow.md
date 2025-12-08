# Luồng kết nối Gateway ↔ ECM

Tài liệu này mô tả cách App Gateway giao tiếp với monolith ECM trong môi trường phát triển và cách phục vụ giao diện người dùng.

## Kiến trúc tổng quát

```text
[Trình duyệt] → [App Gateway]
                  ├─ BFF endpoints (.NET)
                  ├─ Reverse proxy → ECM (HTTP)
                  └─ Static UI (Vite build)
```

- App Gateway chạy trên ASP.NET Core và chịu trách nhiệm xác thực, tổng hợp API (BFF) và reverse proxy tới ECM.
- Các request từ trình duyệt được gửi tới Gateway, sau đó Gateway sử dụng `IEcmApiClient` để gọi tới monolith (`Services:Ecm`).
- UI (SPA) được build sẵn trong `src/AppGateway/ui/dist` và được Gateway phục vụ trực tiếp.
- Các API viewer mới (`GET /api/viewer/{versionId}` và proxy `word`/`excel`) vẫn forward token người dùng để tải metadata/stream từ ECM,
  giúp frontend chỉ cần gọi Gateway.

## Cấu hình kết nối

| Thành phần | Biến cấu hình | Giá trị mặc định | Ghi chú |
|------------|---------------|------------------|--------|
| App Gateway → ECM | `Services:Ecm` | `http://localhost:8080` | Cập nhật khi ECM host chạy ở cổng khác. |

Giá trị có thể được thiết lập trong `appsettings.Development.json`, biến môi trường hoặc thông qua Aspire khi chạy toàn hệ thống.

## Trình tự xử lý request

1. Người dùng truy cập `http://localhost:8080` (hoặc cổng Gateway đang chạy).
2. Gateway phục vụ bundle SPA từ `src/AppGateway/ui/dist` (đường dẫn được ánh xạ làm `webroot`).
3. SPA gọi API nội bộ (ví dụ `GET /api/ecm/documents`).
4. Các controller trong Gateway sử dụng `IEcmApiClient` để forward request xuống ECM bằng HttpClient.
5. ECM xử lý nghiệp vụ và trả kết quả → Gateway trả lại response cho UI.

## Khắc phục lỗi UI không tải được

- Đảm bảo đã build SPA: `cd src/AppGateway/ui && npm install && npm run build`.
- Chạy Gateway bằng `dotnet run --project src/AppGateway/AppGateway.Api/AppGateway.Api.csproj`.
- Kiểm tra log khởi động: Gateway sẽ thông báo đường dẫn webroot và expose `index.html`.
- Nếu sử dụng Aspire, chắc chắn service `svc-app-gateway` và `app-ecm` đều ở trạng thái `Running`.
- Dùng endpoint health check: `curl http://localhost:8080/health` để xác nhận Gateway hoạt động.

## Tham khảo thêm

- [ARCHITECT.md](../ARCHITECT.md) – mô tả kiến trúc tổng thể.
- [README](../README.md) – hướng dẫn khởi chạy hạ tầng và các service.
