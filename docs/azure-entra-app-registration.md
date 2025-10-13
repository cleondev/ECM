# Đăng ký ứng dụng Azure Entra ID cho ECM

Tài liệu này tổng hợp các thông tin cần thiết để đăng ký **Application** trên Azure Entra ID phục vụ cho hệ thống ECM. Các giá trị dưới đây lấy từ cấu hình mặc định trong repo, bạn cần thay thế bằng thông tin thực tế của tenant khi triển khai.

## 1. Ứng dụng API cho ECM Host

| Thuộc tính | Giá trị gợi ý | Nguồn cấu hình | Ghi chú |
|------------|---------------|----------------|--------|
| **Tên ứng dụng** | `ECM Host API` | – | Có thể tùy chỉnh theo chuẩn đặt tên nội bộ. |
| **Account type** | Accounts in this organizational directory only (Single tenant) | – | Phù hợp khi triển khai nội bộ trong 1 tenant. |
| **Redirect URI (loại Web)** | `https://localhost:5001/signin-oidc` _(tùy chọn)_ | – | Chỉ cần khi sử dụng OpenID Connect interactive flow. Với API thuần bearer token có thể bỏ qua. |
| **Identifier (App ID URI)** | `api://ecm-host` | `appsettings.json` (`AzureAd:Audience`) | Đảm bảo unique trong tenant, có thể đổi sang URI chuẩn của tổ chức. |
| **Expose API → Scope mặc định** | `api://ecm-host/.default` | Suy ra từ App ID URI | Sử dụng cho client khi yêu cầu access token. |
| **Client ID (Application ID)** | Tự sinh sau khi đăng ký | – | Thay vào cấu hình `AzureAd:ClientId` của `ECM.Host`. |
| **Tenant ID** | `<tenant-guid>` | `AzureAd:TenantId` | Điền GUID tenant thực tế. |
| **Domain** | `<tenant-domain>.onmicrosoft.com` | `AzureAd:Domain` | Giúp SDK xác định authority. |
| **API permissions** | `User.Read` (Microsoft Graph – delegated, tùy nhu cầu) | – | Thêm các scope cần thiết nếu API gọi dịch vụ khác. |

> Sau khi đăng ký, cập nhật `src/ECM/ECM.Host/appsettings.json` hoặc cấu hình bí mật tương ứng với giá trị `ClientId`, `TenantId`, `Domain` và `Audience` (nếu đổi App ID URI).【F:src/ECM/ECM.Host/appsettings.json†L30-L39】

## 2. Ứng dụng API cho App Gateway

App Gateway đóng vai trò BFF và reverse proxy. Cần một ứng dụng Azure Entra ID riêng nếu muốn phân biệt resource API.

| Thuộc tính | Giá trị gợi ý | Nguồn cấu hình | Ghi chú |
|------------|---------------|----------------|--------|
| **Tên ứng dụng** | `ECM Gateway API` | – | Có thể đổi tên. |
| **Account type** | Accounts in this organizational directory only | – | Giống ECM Host. |
| **Identifier (App ID URI)** | `api://ecm-gateway` | `appsettings.json` (`AzureAd:Audience`) | Đồng bộ với cấu hình ứng dụng. |
| **Client ID (Application ID)** | Tự sinh sau khi đăng ký | – | Gán vào `AzureAd:ClientId` của App Gateway. |
| **Tenant ID** | `<tenant-guid>` | `AzureAd:TenantId` | Trùng với tenant. |
| **Domain** | `<tenant-domain>.onmicrosoft.com` | `AzureAd:Domain` | |
| **API permissions** | Tối thiểu `ECM Host API/.default` (application permission) nếu Gateway gọi xuống ECM bằng chứng thực ứng dụng. | – | Cấp quyền thông qua mục **API permissions**. |

> Cập nhật các giá trị vào `src/AppGateway/AppGateway.Api/appsettings.json` hoặc secret tương ứng sau khi đăng ký.【F:src/AppGateway/AppGateway.Api/appsettings.json†L27-L36】

## 3. Trao quyền giữa các ứng dụng

1. Sau khi đăng ký cả hai ứng dụng, mở trang **Expose an API** của `ECM Host API` và tạo scope mặc định (ví dụ `user_impersonation`).
2. Tại ứng dụng `ECM Gateway API`, thêm quyền truy cập vào scope vừa tạo trong mục **API permissions** → **Add a permission** → **My APIs**.
3. Nếu Gateway sử dụng client credentials flow (application permission) để gọi ECM Host, tạo client secret tại `Certificates & secrets` của Gateway và cấu hình dưới dạng biến môi trường (ví dụ `AzureAd:ClientSecret`).

## 4. Thông tin cho front-end (SPA)

Hiện thư mục `src/AppGateway/ui` chưa cấu hình MSAL. Khi bổ sung, cần sử dụng cùng Tenant ID, Client ID (ứng dụng đại diện SPA hoặc dùng chung App Gateway) và redirect URI tương ứng với domain public của Gateway.

## 5. Lưu ý cấu hình runtime

- Không commit trực tiếp `ClientSecret`. Sử dụng Secret Manager của .NET (`dotnet user-secrets`), Azure Key Vault hoặc biến môi trường trong môi trường triển khai.
- Các giá trị trong `appsettings.json` chỉ mang tính minh hoạ (domain `contoso.onmicrosoft.com`, GUID `00000000-0000-0000-0000-000000000000`). Luôn thay bằng giá trị thực tế trước khi chạy production.
- Nếu thay đổi `Audience` (App ID URI), nhớ cập nhật lại giá trị tương ứng ở cả phía phát hành token và client tiêu thụ token.

