# App Gateway

Lớp BFF/Reverse Proxy cho phép client giao tiếp với monolith ECM thông qua một điểm truy cập hợp nhất.

## Projects

- `AppGateway.Api` – ASP.NET Core host cung cấp endpoint tổng hợp và reverse proxy.
- `AppGateway.Infrastructure` – Định nghĩa các client gọi xuống `ecm` monolith với chính sách resilience.
- `AppGateway.Contracts` – DTO dùng chung giữa API và UI.
- `ui/` – Chứa mã nguồn SPA sẽ tiêu thụ API (chưa hiện thực).

## Cấu hình

Biến cấu hình quan trọng:

```json
{
  "Services": {
    "Ecm": "http://localhost:8080"
  }
}
```

Có thể đặt trong `appsettings.json` hoặc thông qua biến môi trường `Services__Ecm` để chỉ định địa chỉ của monolith.
