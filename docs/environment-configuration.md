# Cấu hình biến môi trường và secrets

Tài liệu này tổng hợp các biến môi trường/secrets đã sử dụng trong hệ thống ECM, đồng thời hướng dẫn cách thiết lập cho môi trường phát triển local. Các service .NET đều dùng chung `ServiceDefaults` nên có thể đọc cấu hình từ:

- **Biến môi trường dạng chuẩn** với cú pháp `Section__Nested__Key` (ví dụ `AzureAd__ClientSecret`).
- **Biến môi trường tiền tố `ECM_`** như `ECM_FileStorage__BucketName`. Trong runtime, tiền tố sẽ được tự động lược bỏ và ánh xạ về đúng section cấu hình.
- **User secrets** (`dotnet user-secrets`) ở môi trường Development để tránh commit secrets vào repo.

> Khi tồn tại cả biến môi trường và cấu hình trong `appsettings*.json`, giá trị từ biến môi trường sẽ được ưu tiên hơn.

## Bảng ánh xạ secrets ↔ cấu hình

### App Gateway (`src/AppGateway/AppGateway.Api`)

| Secret/Azure Key Vault | Khóa cấu hình (.NET options) | Ghi chú |
|------------------------|-------------------------------|--------|
| `AzureAd__ClientSecret` hoặc `ECM_AzureAd__ClientSecret` | `AzureAd:ClientSecret` | Bắt buộc để flow OpenID Connect hoạt động. Khuyến nghị lưu bằng `dotnet user-secrets` trong môi trường DEV. |
| `Services__Ecm` | `Services:Ecm` | URL reverse proxy tới `ECM.Host`. Có thể trỏ về `https://localhost:8080` khi chạy local. |

### ECM Host (`src/ECM/ECM.Host`)

| Secret/Azure Key Vault | Khóa cấu hình | Ghi chú |
|------------------------|---------------|--------|
| `ECM_Database__Connections__iam` | `ConnectionStrings:IAM` | Chuỗi kết nối module IAM. Secret vẫn giữ tên cũ (schema IAM) và được pipeline copy sang tên module chuẩn. |
| `ECM_Database__Connections__doc` | `ConnectionStrings:Document`, `ConnectionStrings:File` | Chuỗi kết nối chung cho module Document và File. |
| `ECM_Database__Connections__wf` | `ConnectionStrings:Workflow` | Chuỗi kết nối module Workflow. |
| `ECM_Database__Connections__search` | `ConnectionStrings:Search` | Chuỗi kết nối module Search. |
| `ECM_Database__Connections__ocr` | `ConnectionStrings:Ocr` | Chuỗi kết nối module OCR. |
| `ECM_Database__Connections__ops` | `ConnectionStrings:Operations` | Chuỗi kết nối module Operations (outbox, audit, notifications, retention). |
| `ECM_Database__Connections__webhook` | `ConnectionStrings:Webhook` | Chuỗi kết nối module Webhook (ghi nhận lịch sử delivery). |
| `ECM_FileStorage__BucketName` | `FileStorage:BucketName` | Tên bucket MinIO/S3. |
| `ECM_FileStorage__ServiceUrl` | `FileStorage:ServiceUrl` | Endpoint MinIO/S3 (ví dụ `http://localhost:9000`). |
| `ECM_FileStorage__AccessKeyId` | `FileStorage:AccessKeyId` | Access key MinIO/S3. |
| `ECM_FileStorage__SecretAccessKey` | `FileStorage:SecretAccessKey` | Secret key MinIO/S3. |
| `ECM_Workflow__Camunda__BaseUrl` | `Workflow:Camunda:BaseUrl` | Base URL Camunda REST. |
| `ECM_Workflow__Camunda__TenantId` | `Workflow:Camunda:TenantId` | Tenant Camunda dùng để phân tách quy trình. |
| `ECM_AzureAd__ClientSecret` *(tùy chọn)* | `AzureAd:ClientSecret` | Chỉ cần khi host phải gọi các API bảo vệ bằng client credential. Có thể để trống nếu chưa dùng. |

### Workers & service phụ trợ

Các worker trong `src/Workers/*` dùng chung cấu hình với `ECM.Host`. Chỉ cần set các biến `ConnectionStrings__Operations` (hoặc secret tương ứng `ECM_Database__Connections__ops`), `ConnectionStrings__Webhook` (nếu chạy `WebhookDispatcher`), `Kafka__*` (nếu có), và `ECM_FileStorage__*` tùy vào chức năng worker. Các biến có tiền tố `ECM_` đều được tự động nạp nhờ `ServiceDefaults`.

### CI/CD & container registry

| Secret | Mục đích |
|--------|----------|
| `HARBOR_USER` | Service account để push/pull image lên Harbor registry. |
| `HARBOR_PASS` | Mật khẩu tương ứng với `HARBOR_USER`. |

## Thiết lập nhanh cho môi trường development local

1. **Khởi tạo secrets cho App Gateway và (nếu cần) ECM Host** bằng `dotnet user-secrets`:

   ```bash
   dotnet user-secrets set "AzureAd:ClientSecret" "<gateway-client-secret>" \
     --project src/AppGateway/AppGateway.Api/AppGateway.Api.csproj

   # Nếu ECM.Host cần gọi API Azure AD qua client credential:
   dotnet user-secrets set "AzureAd:ClientSecret" "<ecm-host-client-secret>" \
     --project src/ECM/ECM.Host/ECM.Host.csproj
   ```

   Các giá trị này chỉ tồn tại trên máy cá nhân, không xuất hiện trong source control.

2. **Tạo file `.env.development` (không commit)** ở thư mục gốc để gom các biến môi trường còn lại:

   ```bash
   cat <<'ENV' > .env.development
   # Kết nối database (khớp với deploy/compose.yml)
   ConnectionStrings__IAM=Host=localhost;Port=5432;Database=ecm_iam;Username=ecm;Password=ecm
   ConnectionStrings__Document=Host=localhost;Port=5432;Database=ecm_doc;Username=ecm;Password=ecm
   ConnectionStrings__File=Host=localhost;Port=5432;Database=ecm_doc;Username=ecm;Password=ecm
   ConnectionStrings__Workflow=Host=localhost;Port=5432;Database=ecm_wf;Username=ecm;Password=ecm
   ConnectionStrings__Search=Host=localhost;Port=5432;Database=ecm_search;Username=ecm;Password=ecm
   ConnectionStrings__Ocr=Host=localhost;Port=5432;Database=ecm_ocr;Username=ecm;Password=ecm
   ConnectionStrings__Operations=Host=localhost;Port=5432;Database=ecm_ops;Username=ecm;Password=ecm
   ConnectionStrings__Webhook=Host=localhost;Port=5432;Database=ecm_webhook;Username=ecm;Password=ecm

   # MinIO/S3
   ECM_FileStorage__BucketName=ecm-files
   ECM_FileStorage__ServiceUrl=http://localhost:9000
   ECM_FileStorage__AccessKeyId=minio
   ECM_FileStorage__SecretAccessKey=miniominio

   # Camunda workflow
   ECM_Workflow__Camunda__BaseUrl=http://localhost:8080/engine-rest
   ECM_Workflow__Camunda__TenantId=default

   # Routing service
   Services__Ecm=http://localhost:8080
   ENV
   ```

   Sử dụng `source .env.development` (Linux/macOS) hoặc `Get-Content .env.development | foreach { if ($_ -match '^(.*?)=(.*)$') { Set-Item -Path Env:$($matches[1]) -Value $matches[2] } }` trên PowerShell để nạp nhanh trước khi chạy `dotnet run`.

3. **Nếu dùng Docker Compose trong thư mục `deploy/`**, có thể truyền trực tiếp file `.env.development`:

   ```bash
   docker compose --env-file ../.env.development -f deploy/compose.yml up -d
   ```

   Docker sẽ inject các biến như `ECM_FileStorage__*` cho container .NET khi cần triển khai chung.

4. **Giữ secrets ngoài repo**: thêm `.env.development` vào `.gitignore` (đã được cấu hình sẵn) và không commit file chứa secret. Với các môi trường khác (Staging/Production), nên sử dụng Azure Key Vault, Kubernetes Secret hoặc Azure App Configuration để lưu trữ.

Làm theo các bước trên sẽ giúp đồng bộ dữ liệu giữa Azure secrets và cấu hình ứng dụng, đồng thời chuẩn bị được bộ biến môi trường nhất quán cho quá trình phát triển local.
