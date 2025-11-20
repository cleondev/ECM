# ECM file integration sample

Ứng dụng **ASP.NET Core MVC (.NET 9)** này minh họa cách một giao diện web có thể upload file trực tiếp vào ECM qua HTTP API. Bạn có thể cấu hình bearer token tĩnh, đăng nhập bằng Azure SSO thông qua module **Authentication** ở **AppGateway**, hoặc đăng nhập nền bằng API key qua endpoint `api/iam/auth/on-behalf`.

## Chuẩn bị

1. Cài **.NET SDK 9**.
2. Đảm bảo một instance ECM đang chạy và có thể truy cập qua HTTP (ví dụ `http://localhost:8080/`).
3. Chọn phương thức xác thực:
   - **Bearer token tĩnh**: điền `AccessToken` với token hợp lệ của người dùng.
   - **Azure SSO**: bỏ trống `AccessToken`, bật `UseAzureSso` và cấu hình `AzureAd`/`AuthenticationScope` giống AppGateway.
   - **auth/on-behalf**: dùng API key (`X-Api-Key`) của AppGateway để đăng nhập nền cho một tài khoản cụ thể. Bật `OnBehalf:Enabled` và cung cấp `UserEmail` hoặc `UserId`.
4. Tạo (hoặc cập nhật) `samples/EcmFileIntegrationSample/appsettings.json` với thông tin kết nối và metadata mặc định. Khi tích hợp Azure SSO, cấu hình thêm scope và thông tin Azure AD giống AppGateway. Khi dùng auth/on-behalf hãy đặt `BaseUrl` trỏ tới AppGateway (ví dụ `https://localhost:5443/`):

```json
{
  "Ecm": {
    "BaseUrl": "http://localhost:8080/",
    "AccessToken": "<bearer token>",
    "UseAzureSso": true,
    "AuthenticationScope": "api://istsvn.onmicrosoft.com/ecm-host/Access.All",
    "OnBehalf": {
      "Enabled": true,
      "ApiKey": "<app gateway API key>",
      "UserEmail": "user@domain.com",
      "UserId": "<optional user GUID>"
    },
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
>
> Với `OnBehalf.Enabled=true`, ứng dụng sẽ tự động gọi `POST api/iam/auth/on-behalf` (kèm `X-Api-Key`) để đăng nhập nền cho `UserEmail`/`UserId`, sau đó dùng cookie trả về cho các API (upload, lấy profile, tải file).

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
2. Upload file qua `POST /api/ecm/documents` (hoặc `POST /api/documents` khi tích hợp AppGateway + auth/on-behalf) bằng `multipart/form-data` cùng metadata (`ownerId`, `createdBy`, `docType`, `status`, `sensitivity`).
3. Hiển thị thông tin tài liệu, phiên bản mới nhất và presigned URL tải file (`GET /api/ecm/files/download/{versionId}` hoặc `GET /api/documents/files/download/{versionId}`) ngay trên giao diện.
