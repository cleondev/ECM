# Azure Pipelines – Build & Deploy ECM bằng Docker

Pipeline `azure-pipelines.yml` được dùng để build các Docker image của ECM, push lên Harbor registry nội bộ và triển khai stack Docker Compose trên server sản xuất thông qua SSH.

## Kiến trúc tổng quan

Pipeline bao gồm hai stage chạy nối tiếp trên self-hosted agent pool `ECM_CI`:

1. **Build & Push Docker Images**
   - Checkout toàn bộ repo (không cắt lịch sử) để có thể tính toán tag theo branch và build context chính xác.
   - Cài .NET 9 SDK và Node.js 20 cho các bước build.
   - Đăng nhập Harbor bằng service account (`HARBOR_USER`/`HARBOR_PASS`).
   - Build lần lượt các image (`appgateway-api`, `ecm-host`, `notify-worker`, `outbox-worker`, `searchindexer-worker`).
   - Push các image vừa build lên registry `harbor.local:8443/ecm` với tag `$(Build.SourceBranchName)-$(Build.BuildId)`.

2. **Deploy to App Server**
   - Dùng task `SSH@0` để kết nối tới máy chủ (service connection `ecm-appserver-ssh`).
   - Trên server, script sẽ chuyển tới thư mục `/data/ecm`, đăng nhập lại Harbor, kéo các image mới và chạy `docker compose up -d --remove-orphans` để cập nhật stack.

## Biến và secret

Pipeline sử dụng variable group `ECM-SECRETS` để tập trung các thông tin nhạy cảm. Trong bước SSH deploy, script khai báo ba helper `choose_secret`, `require_env` và `optional_env` để:

1. Đọc giá trị đã có sẵn trên máy chủ (nếu có) và giữ nguyên.
2. Bổ sung giá trị từ Azure Variable Group/Key Vault theo chuẩn `ECM_*__*`.
3. Báo lỗi ngay khi thiếu biến bắt buộc và chỉ cảnh báo với biến tùy chọn.

Các biến môi trường được export thành hai nhóm:

**1. Nhóm tương thích với stack Docker Compose cũ**

| Biến môi trường | Secret/nguồn ưu tiên | Ghi chú |
|-----------------|----------------------|--------|
| `MINIO_ENDPOINT` | `ECM_FileStorage__ServiceUrl` | Endpoint MinIO/S3 dạng URL. |
| `MINIO_ACCESS_KEY` | `ECM_FileStorage__AccessKeyId` | Access key MinIO/S3. |
| `MINIO_SECRET_KEY` | `ECM_FileStorage__SecretAccessKey` | Secret key MinIO/S3. |
| `REDPANDA_BROKERS` | `ECM_Kafka__BootstrapServers` | Danh sách broker cho các dịch vụ nền tảng. |
| `Kafka__BootstrapServers` | `ECM_Kafka__BootstrapServers` hoặc giá trị của `REDPANDA_BROKERS` | Worker .NET sử dụng chuẩn `Kafka__*`. |
| `Services__Ecm` | `Services__Ecm` | URL reverse proxy tới `ECM.Host`. |
| `AzureAd__ClientSecret` | `AzureAd__ClientSecret` → `ECM_AzureAd__ClientSecret` | Bắt buộc cho App Gateway thực hiện OpenID Connect. |

**2. Nhóm cấu hình chuẩn ECM (được .NET đọc trực tiếp)**

| Biến môi trường | Bắt buộc | Ghi chú |
|-----------------|----------|--------|
| `ECM_Database__Connections__iam`, `doc`, `wf`, `search`, `ocr`, `ops` → `ConnectionStrings__iam`, `doc`, `wf`, `search`, `ocr`, `ops` | ✔️ | Chuỗi kết nối riêng cho từng schema/module. Pipeline sẽ copy từ secret tiền tố `ECM_` sang `ConnectionStrings__*` để .NET đọc đúng section chuẩn. |
| `ECM_FileStorage__BucketName`, `ECM_FileStorage__ServiceUrl`, `ECM_FileStorage__AccessKeyId`, `ECM_FileStorage__SecretAccessKey` | ✔️ | Thông tin lưu trữ file cho module File. Các giá trị tương ứng cũng được export dưới dạng `FileStorage__*` để giữ tương thích. |
| `ECM_Workflow__Camunda__BaseUrl` | ✔️ | Endpoint Camunda REST cho module Workflow. |
| `ECM_Workflow__Camunda__TenantId` | ⚠️ (tùy chọn) | Nếu bỏ trống, repository sẽ làm việc ở chế độ multi-tenant mặc định của Camunda. Pipeline sẽ log cảnh báo nhưng không dừng job. |
| `ECM_AzureAd__ClientSecret` | ⚠️ (tùy chọn) | Chỉ cần khi ECM Host phải gọi API bảo vệ bằng client credential. Nếu không cung cấp, script sẽ copy giá trị từ `AzureAd__ClientSecret` (nếu có). |

Nếu một biến bắt buộc không được resolve, job Deploy sẽ dừng với thông báo `[deploy] Thiếu biến môi trường bắt buộc`. Các biến tùy chọn thiếu sẽ được liệt kê dưới dạng cảnh báo để bạn theo dõi.

Ngoài các biến trên, hãy đảm bảo variable group còn chứa `HARBOR_USER` và `HARBOR_PASS` (phục vụ bước build/push image). Những thông tin khác mà `docker compose` sử dụng riêng có thể tiếp tục export trong script hoặc quản lý trực tiếp trong Azure Variable Group.

## Tùy biến pipeline

- **Đổi server triển khai**: cập nhật `displayName`, `sshEndpoint` và đường dẫn `cd /data/ecm` trong task `SSH@0` cho phù hợp.
- **Tag hoặc registry khác**: thay đổi biến `REGISTRY` hoặc logic đặt `TAG` trong stage Build.
- **Bổ sung service**: thêm dòng `docker build`/`docker push` trong stage Build, đồng thời cập nhật `docker compose` trên server.
- **Quản lý nhiều môi trường**: nhân bản job `SSH@0` hoặc tách thành nhiều stage Deploy khác nhau (ví dụ Staging, Production) với service connection và secret group riêng.

## Quy trình vận hành

1. Push code lên branch mục tiêu (ví dụ `main`).
2. Stage Build chạy, build & push các image với tag `branch-buildId`.
3. Stage Deploy đăng nhập vào server, đồng bộ image và khởi động lại dịch vụ Docker Compose.
4. Kiểm tra nhật ký `docker compose` hoặc log pipeline để xác nhận deployment thành công.
