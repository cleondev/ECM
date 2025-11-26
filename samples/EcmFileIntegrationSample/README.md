# ECM file integration sample

Ứng dụng **ASP.NET Core MVC (.NET 9)** này minh họa cách một giao diện web có thể upload file trực tiếp vào ECM qua HTTP API. Phần tích hợp HTTP đã được tách thành thư viện NuGet `Ecm.SDK` (đặt tại `src/Shared/Ecm.SDK`) để có thể tái sử dụng cho các ứng dụng khác. Bạn có thể cấu hình bearer token tĩnh, đăng nhập nền bằng API key qua endpoint `api/iam/auth/on-behalf` hoặc dùng luồng OBO (on-behalf-of) dựa trên SSO để đổi token của người dùng thành token ECM AppGateway.

## Chuẩn bị

1. Cài **.NET SDK 9**.
2. Đảm bảo một instance ECM đang chạy và có thể truy cập qua HTTP (ví dụ `http://localhost:8080/`).
3. Chọn phương thức xác thực:
   - **Bearer token tĩnh**: điền `AccessToken` với token hợp lệ của người dùng.
   - **auth/on-behalf qua API key**: dùng API key (`X-Api-Key`) của AppGateway để đăng nhập nền cho một tài khoản cụ thể. Bật `OnBehalf:Enabled` và cung cấp `UserEmail` hoặc `UserId`.
   - **OBO qua SSO**: bật `OnBehalf:Sso:Enabled=true` để thư viện tự thực hiện MSAL `AcquireTokenOnBehalfOf` với application (client) của Sample. Bạn cần truyền token người dùng nhận được từ đăng nhập SSO (`UserAccessToken`) và thông tin ứng dụng/authority để đổi thành token cho AppGateway (scope `api://<appgateway-client-id>/.default`). Khi SSO bật, thư viện dùng bearer token và bỏ qua bước đăng nhập on-behalf bằng API key.
4. Tạo (hoặc cập nhật) `samples/EcmFileIntegrationSample/appsettings.json` với thông tin kết nối và metadata mặc định. Khi dùng auth/on-behalf hãy đặt `BaseUrl` trỏ tới AppGateway (ví dụ `https://localhost:5443/`). Có thể cấu hình nhiều user trong `EcmUsers` để chuyển nhanh ngay trên giao diện (ứng dụng luôn chọn user đầu tiên khi mở trang):

```json
{
  "Ecm": {
    "BaseUrl": "http://localhost:8080/",
    "AccessToken": "<bearer token>",
    "OnBehalf": {
      "Enabled": true,
      "ApiKey": "<app gateway API key>",
      "UserEmail": "user@domain.com",
      "UserId": "<optional user GUID>",
      "Sso": {
        "Enabled": false,
        "Authority": "https://login.microsoftonline.com/<tenant-id>",
        "ClientId": "<application-client-id>",
        "ClientSecret": "<application-client-secret>",
        "Scopes": [
          "api://<appgateway-client-id>/.default"
        ],
        "UserAccessToken": "<access token người dùng sau khi đăng nhập SSO>"
      }
    },
    "OwnerId": "<optional user GUID to set owner>",
    "CreatedBy": "<optional user GUID to set creator>",
    "DocType": "General",
    "Status": "Draft",
    "Sensitivity": "Internal"
 },
  "EcmUsers": [
    {
      "Key": "user1",
      "DisplayName": "User on-behalf nền",
      "Settings": {
        "BaseUrl": "http://localhost:5090",
        "OnBehalf": {
          "Enabled": true,
          "ApiKey": "admin123",
          "UserEmail": "user1@example.com"
        }
      }
    },
    {
      "Key": "user2",
      "DisplayName": "User bearer token",
      "Settings": {
        "BaseUrl": "http://localhost:5090",
        "AccessToken": "<access token>"
      }
    }
  ]
}
```

> Nếu không chỉ định `OwnerId`/`CreatedBy`, ứng dụng sẽ dùng thông tin người dùng trong token (`/api/iam/profile`).
>
> Với `OnBehalf.Enabled=true`, ứng dụng sẽ tự động gọi `POST api/iam/auth/on-behalf` (kèm `X-Api-Key`) để đăng nhập nền cho `UserEmail`/`UserId`, sau đó dùng cookie trả về cho các API (upload, lấy profile, tải file). Khi `OnBehalf:Sso:Enabled=true`, thư viện bỏ qua bước này và lấy bearer token AppGateway bằng MSAL OBO.

## Dùng lại SDK `Ecm.SDK`

1. Thêm reference tới dự án (hoặc gói NuGet sau khi publish) `Ecm.SDK`.
2. Đăng ký DI trong `Program.cs`:

   ```csharp
   builder.Services.AddEcmSdk(builder.Configuration);
   ```

   Thư viện sẽ bind cấu hình từ section `Ecm`, tạo `HttpClient` có `UserAgent` mặc định và tự xử lý cookie, bearer token, đăng nhập on-behalf hoặc đổi token SSO.
3. Inject `EcmFileClient` ở nơi cần dùng và gọi các method như `UploadDocumentAsync`, `GetDownloadUriAsync`, `ListTagsAsync`, `ListDocumentsAsync`, v.v.

`EcmIntegrationOptions` hỗ trợ bearer token tĩnh (`AccessToken`), đăng nhập on-behalf bằng API key (`OnBehalf:*`) và đổi token SSO (`OnBehalf:Sso:*`). Các validate cơ bản (base URL, API key, định danh người dùng, cấu hình SSO) được thực thi khi khởi động.

## Chạy thử

```bash
# Build và chạy ứng dụng MVC
dotnet run --project samples/EcmFileIntegrationSample/EcmFileIntegrationSample.csproj
```

Truy cập giao diện tại địa chỉ được in ra console (mặc định `https://localhost:5001` hoặc `http://localhost:5000`).

Trên giao diện, bạn có thể:

1. Quan sát base URL, trạng thái access token (đã cấu hình/chưa cấu hình).
2. Nhập metadata (doc type, status, sensitivity, document type ID, owner ID, created by, tiêu đề).
3. Chọn tag đa chọn (nếu muốn gán ngay khi upload).
4. Chọn file bất kỳ (PDF, hình ảnh, văn bản, …) và nhấn **Upload**.

Ứng dụng sẽ:

1. Gọi `GET /api/iam/profile` để xác định người dùng (để dùng làm owner/creator nếu để trống).
2. Upload file qua `POST /api/ecm/documents` (hoặc `POST /api/documents` khi tích hợp AppGateway + auth/on-behalf) bằng `multipart/form-data` cùng metadata (`ownerId`, `createdBy`, `docType`, `status`, `sensitivity`).
3. Hiển thị thông tin tài liệu, phiên bản mới nhất và presigned URL tải file (`GET /api/ecm/files/download/{versionId}` hoặc `GET /api/documents/files/download/{versionId}`) ngay trên giao diện.

Ngoài upload, trang mẫu còn cung cấp form mẫu cho các API ECM:

- CRUD tag (`GET/POST/PUT/DELETE /api/ecm/tags`) để tạo, cập nhật, xoá tag label.
- Liệt kê tài liệu (`GET /api/ecm/documents`), cập nhật metadata (`PUT /api/ecm/documents/{id}`) và xoá tài liệu (`DELETE /api/ecm/documents/{id}`).
