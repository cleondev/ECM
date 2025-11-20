# ECM file integration sample

Dự án console .NET 9 này minh họa cách một ứng dụng khác có thể upload file vào ECM qua HTTP API sẵn có.

## Chuẩn bị

1. Cài **.NET SDK 9**.
2. Đảm bảo một instance ECM đang chạy và có thể truy cập qua HTTP (ví dụ `http://localhost:8080/`).
3. Lấy bearer token hợp lệ cho tài khoản cần upload file rồi điền vào cấu hình (phần `AccessToken`).
   - Token có thể lấy từ Azure AD hoặc nguồn định danh đang bảo vệ ECM.
4. Tạo (hoặc cập nhật) `samples/EcmFileIntegrationSample/appsettings.json` với thông tin kết nối:

```json
{
  "Ecm": {
    "BaseUrl": "http://localhost:8080/",
    "AccessToken": "<bearer token>",
    "OwnerId": "<optional user GUID to set owner>",
    "CreatedBy": "<optional user GUID to set creator>",
    "FilePath": "sample-data/hello-world.txt",
    "DocType": "General",
    "Status": "Draft",
    "Sensitivity": "Internal"
  }
}
```

> Nếu không chỉ định `OwnerId`/`CreatedBy`, ứng dụng sẽ dùng thông tin người dùng trong token (`/api/iam/profile`).

## Chạy thử

```bash
# Build và chạy sample
dotnet run --project samples/EcmFileIntegrationSample/EcmFileIntegrationSample.csproj
```

Ứng dụng sẽ:

1. Gọi `GET /api/iam/profile` để xác định người dùng.
2. Upload file qua `POST /api/ecm/documents` bằng `multipart/form-data` cùng metadata (`ownerId`, `createdBy`, `docType`, `status`, `sensitivity`).
3. Ghi log thông tin tài liệu, phiên bản mới nhất và presigned URL tải file (`GET /api/ecm/files/download/{versionId}`).

Có thể thay đổi `FilePath` tới file bất kỳ (PDF, hình ảnh, …) để kiểm thử lưu trữ thực tế.
