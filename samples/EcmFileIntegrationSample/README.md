# ECM file integration sample

Ứng dụng **ASP.NET Core MVC (.NET 9)** này minh họa cách một giao diện web có thể upload file trực tiếp vào ECM qua HTTP API sẵn có mà **không cần đăng nhập**. Người dùng chỉ cần cấu hình bearer token một lần trong `appsettings.json`.

## Chuẩn bị

1. Cài **.NET SDK 9**.
2. Đảm bảo một instance ECM đang chạy và có thể truy cập qua HTTP (ví dụ `http://localhost:8080/`).
3. Lấy bearer token hợp lệ cho tài khoản cần upload file rồi điền vào cấu hình (phần `AccessToken`). Token có thể lấy từ Azure AD hoặc nguồn định danh đang bảo vệ ECM.
4. Tạo (hoặc cập nhật) `samples/EcmFileIntegrationSample/appsettings.json` với thông tin kết nối và metadata mặc định:

```json
{
    "Ecm": {
      "BaseUrl": "http://localhost:8080/",
      "AccessToken": "<bearer token>",
      "OwnerId": "<optional user GUID to set owner>",
      "CreatedBy": "<optional user GUID to set creator>",
      "DocType": "General",
      "Status": "Draft",
      "Sensitivity": "Internal"
    }
  }
```

> Nếu không chỉ định `OwnerId`/`CreatedBy`, ứng dụng sẽ dùng thông tin người dùng trong token (`/api/iam/profile`).

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
