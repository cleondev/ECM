# ECM file integration sample

Ứng dụng **ASP.NET Core MVC (.NET 9)** này minh họa cách một giao diện web có thể upload file trực tiếp vào ECM qua HTTP API. Phần tích hợp HTTP đã được tách thành thư viện NuGet `Ecm.Sdk` (đặt tại `samples/Ecm.Sdk`) để có thể tái sử dụng cho các ứng dụng khác. Bạn có thể đăng nhập nền bằng API key qua endpoint `api/iam/auth/on-behalf` hoặc dùng luồng OBO (on-behalf-of) dựa trên SSO để đổi token của người dùng thành token ECM AppGateway.

## Chuẩn bị

1. Cài **.NET SDK 9**.
2. Đảm bảo một instance ECM đang chạy và có thể truy cập qua HTTP (ví dụ `http://localhost:8080/`).
3. Chọn phương thức xác thực on-behalf (thư viện ưu tiên SSO, nếu bật `Sso:Enabled=true` và lấy được token thì sẽ bỏ qua bước đăng nhập API key; nếu token SSO thiếu sẽ fallback sang API key nếu được cấu hình):
   - **auth/on-behalf qua API key**: bật `Ecm:ApiKey:Enabled=true` và điền `Ecm:ApiKey:ApiKey` (API key `X-Api-Key` **duy nhất cho ứng dụng**). Email người dùng đăng nhập nền được lấy từ dropdown `EcmUsers`.
   - **OBO qua SSO**: cấu hình một lần ở `Ecm:Sso` (authority, client, client secret, scope) và bật `Ecm:Sso:Enabled=true`. Thư viện sẽ dùng token người dùng (truyền vào `Ecm:Sso:UserAccessToken`) để thực hiện MSAL `AcquireTokenOnBehalfOf` lấy bearer token AppGateway (scope `api://<appgateway-client-id>/.default`).
4. Tạo (hoặc cập nhật) `samples/EcmFileIntegrationSample/appsettings.json` với thông tin kết nối và metadata mặc định. Phần `Ecm` là cấu hình chung (base URL, API key, SSO). `EcmUsers` chỉ cần danh sách người dùng (email + tên hiển thị) để chọn nhanh trên giao diện; API key và cấu hình SSO không lặp lại tại đây. Khi dùng auth/on-behalf hãy đặt `BaseUrl` trỏ tới AppGateway (ví dụ `https://localhost:5443/`). Ứng dụng luôn chọn user đầu tiên khi mở trang và bạn có thể chuyển user nhanh qua dropdown:

  ```json
  {
    "Ecm": {
      "BaseUrl": "http://localhost:5090",
      "ApiKey": {
        "Enabled": false,
        "ApiKey": "<app gateway API key>"
      },
      "Sso": {
        "Enabled": false,
        "Authority": "https://login.microsoftonline.com/<tenant-id>",
        "ClientId": "<application-client-id>",
        "ClientSecret": "<application-client-secret>",
        "Scopes": [
          "api://<appgateway-client-id>/.default"
        ]
      },
      "OwnerId": "",
      "CreatedBy": "",
      "DocType": "General",
      "Status": "Draft",
      "Sensitivity": "Internal"
    },
    "EcmUsers": [
      {
        "DisplayName": "User A",
        "Email": "user1@example.com"
      },
      {
        "DisplayName": "User B",
        "Email": "user2@example.com"
      },
      {
        "DisplayName": "User C",
        "Email": "user3@example.com"
      }
    ]
  }
  ```

> Nếu không chỉ định `OwnerId`/`CreatedBy`, ứng dụng sẽ dùng thông tin người dùng trong token (`/api/iam/profile`).

> Với `ApiKey.Enabled=true`, ứng dụng sẽ tự động gọi `POST api/iam/auth/on-behalf` (kèm `X-Api-Key` trong cấu hình chung) để đăng nhập nền cho email đang chọn trong dropdown `EcmUsers`, sau đó dùng cookie trả về cho các API (upload, lấy profile, tải file). Khi `Sso.Enabled=true`, thư viện ưu tiên bearer token AppGateway qua MSAL OBO dựa trên `Sso:UserAccessToken`; nếu không lấy được token, SDK sẽ fallback đăng nhập bằng API key (nếu bật).

## Dùng lại SDK `Ecm.Sdk`

1. Thêm reference tới dự án (hoặc gói NuGet sau khi publish) `Ecm.Sdk`.
2. Đăng ký DI trong `Program.cs`:

   ```csharp
   builder.Services.AddEcmSdk(builder.Configuration);
   ```

   Thư viện sẽ bind cấu hình từ section `Ecm`, tạo `HttpClient` có `UserAgent` mặc định và tự xử lý cookie, bearer token, đăng nhập on-behalf hoặc đổi token SSO.
3. Inject `EcmFileClient` ở nơi cần dùng và gọi các method như `UploadDocumentAsync`, `GetDownloadUriAsync`, `ListTagsAsync`, `ListDocumentsAsync`, v.v.

`EcmIntegrationOptions` hỗ trợ đăng nhập on-behalf bằng API key (`ApiKey:*`) và đổi token SSO (`Sso:*`). Các validate cơ bản (base URL, API key, định danh người dùng, cấu hình SSO) được thực thi khi khởi động.

## Chạy thử

```bash
# Build và chạy ứng dụng MVC
dotnet run --project samples/EcmFileIntegrationSample/EcmFileIntegrationSample.csproj
```

Truy cập giao diện tại địa chỉ được in ra console (mặc định `https://localhost:5001` hoặc `http://localhost:5000`).

Trên giao diện, bạn có thể:

1. Quan sát base URL, trạng thái on-behalf (API key/SSO đang bật hay chưa).
2. Nhập metadata (doc type, status, sensitivity, document type ID, owner ID, created by, tiêu đề).
3. Chọn tag đa chọn (nếu muốn gán ngay khi upload).
4. Chọn file bất kỳ (PDF, hình ảnh, văn bản, …) và nhấn **Upload**.

Trang **Upload hàng loạt** minh hoạ API `POST /api/ecm/documents/batch` (hoặc `/api/documents/batch` khi đi qua AppGateway + on-behalf). Bạn có thể chọn nhiều file trong một request duy nhất, SDK sẽ đóng gói metadata (doc type, status, sensitivity, tag, flow definition) và hiển thị danh sách tài liệu đã tạo kèm các file lỗi (nếu có).

Ứng dụng sẽ:

1. Gọi `GET /api/iam/profile` để xác định người dùng (để dùng làm owner/creator nếu để trống).
2. Upload file qua `POST /api/ecm/documents` (hoặc `POST /api/documents` khi tích hợp AppGateway + auth/on-behalf) bằng `multipart/form-data` cùng metadata (`ownerId`, `createdBy`, `docType`, `status`, `sensitivity`).
3. Hiển thị thông tin tài liệu, phiên bản mới nhất và presigned URL tải file (`GET /api/ecm/files/download/{versionId}` hoặc `GET /api/documents/files/download/{versionId}`) ngay trên giao diện.

Ngoài upload, trang mẫu còn cung cấp form mẫu cho các API ECM:

- CRUD tag (`GET/POST/PUT/DELETE /api/ecm/tags`) để tạo, cập nhật, xoá tag label.
- Liệt kê tài liệu (`GET /api/ecm/documents`), cập nhật metadata (`PUT /api/ecm/documents/{id}`) và xoá tài liệu (`DELETE /api/ecm/documents/{id}`).
- Xem chi tiết tài liệu (`GET /api/ecm/documents/{id}`) và lấy presigned URL download cho phiên bản mới nhất.
- Tải trực tiếp nội dung file qua API preview (`GET /api/ecm/files/preview/{versionId}`) bằng cách nhập Version ID.
