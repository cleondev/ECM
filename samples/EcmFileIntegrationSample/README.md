# ECM file integration sample

Ứng dụng **ASP.NET Core MVC (.NET 9)** này minh họa cách một giao diện web có thể upload file trực tiếp vào ECM qua HTTP API. Bạn có thể cấu hình bearer token tĩnh hoặc đăng nhập bằng Azure SSO thông qua module **Authentication** ở **AppGateway**.

## Chuẩn bị

1. Cài **.NET SDK 9**.
2. Đảm bảo một instance ECM đang chạy và có thể truy cập qua HTTP (ví dụ `http://localhost:8080/`).
3. Lấy bearer token hợp lệ cho tài khoản cần upload file rồi điền vào cấu hình (phần `AccessToken`). Token có thể lấy từ Azure AD hoặc nguồn định danh đang bảo vệ ECM. Nếu sử dụng Azure SSO với AppGateway, bạn có thể bỏ trống `AccessToken` và bật `UseAzureSso`.
4. Tạo (hoặc cập nhật) `samples/EcmFileIntegrationSample/appsettings.json` với thông tin kết nối và metadata mặc định. Khi tích hợp Azure SSO, cấu hình thêm scope và thông tin Azure AD giống AppGateway:

```json
{
  "Ecm": {
    "BaseUrl": "http://localhost:8080/",
    "AccessToken": "<bearer token>",
    "UseAzureSso": true,
    "AuthenticationScope": "api://istsvn.onmicrosoft.com/ecm-host/Access.All",
    "OwnerId": "<optional user GUID to set owner>",
    "CreatedBy": "<optional user GUID to set creator>",
    "DocType": "General",
    "Status": "Draft",
    "Sensitivity": "Internal"
  },
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "Domain": "istsvn.onmicrosoft.com",
    "TenantId": "b3b40bc6-b026-4f9b-abec-ab701dfeec71",
    "ClientId": "0ea2db84-e191-4676-b804-881f2ed0ef3e",
    "CallbackPath": "/signin-oidc",
    "SignedOutCallbackPath": "/signout-callback-oidc"
  }
}
```

> Nếu không chỉ định `OwnerId`/`CreatedBy`, ứng dụng sẽ dùng thông tin người dùng trong token (`/api/iam/profile`).
>
> Với `UseAzureSso=true`, người dùng sẽ được chuyển tới Azure AD để đăng nhập. Ứng dụng sẽ tự động lấy access token theo `AuthenticationScope` (mặc định là scope ECM mà AppGateway dùng) và gắn vào mọi request.

## Chạy thử

```bash
# Build và chạy ứng dụng MVC
dotnet run --project samples/EcmFileIntegrationSample/EcmFileIntegrationSample.csproj
```

Truy cập giao diện tại địa chỉ được in ra console (mặc định `https://localhost:5001` hoặc `http://localhost:5000`).

Trên giao diện, bạn có thể:

1. Quan sát base URL, trạng thái access token (đã cấu hình/chưa cấu hình).
2. Nhập metadata (doc type, status, sensitivity, document type ID, owner ID, created by, tiêu đề).
3. Chọn file bất kỳ (PDF, hình ảnh, văn bản, …) và nhấn **Upload**.

Ứng dụng sẽ:

1. Gọi `GET /api/iam/profile` để xác định người dùng (để dùng làm owner/creator nếu để trống).
2. Upload file qua `POST /api/ecm/documents` bằng `multipart/form-data` cùng metadata (`ownerId`, `createdBy`, `docType`, `status`, `sensitivity`).
3. Hiển thị thông tin tài liệu, phiên bản mới nhất và presigned URL tải file (`GET /api/ecm/files/download/{versionId}`) ngay trên giao diện.
