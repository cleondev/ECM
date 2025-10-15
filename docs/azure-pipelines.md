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

Pipeline sử dụng variable group `ECM-SECRETS` để tập trung các thông tin nhạy cảm. Script triển khai có hàm `choose_secret` nhằm tự động ánh xạ các khóa secret mới (theo dạng cấu hình phân cấp của ECM) sang các biến môi trường "phẳng" mà stack Docker hiện tại đang dùng.

| Biến môi trường dùng trong compose | Secret ưu tiên | Secret fallback (theo chuẩn ECM) |
|-----------------------------------|----------------|-----------------------------------|
| `DB_CONNSTRING`                   | `DB_CONNSTRING`| `ECM_ConnectionStrings__postgres` |
| `MINIO_ENDPOINT`                  | `MINIO_ENDPOINT` | `ECM_FileStorage__ServiceUrl`     |
| `MINIO_ACCESS_KEY`                | `MINIO_ACCESS_KEY` | `ECM_FileStorage__AccessKeyId`  |
| `MINIO_SECRET_KEY`                | `MINIO_SECRET_KEY` | `ECM_FileStorage__SecretAccessKey` |
| `REDPANDA_BROKERS`                | `REDPANDA_BROKERS` | `ECM_Kafka__BootstrapServers`  |

Bạn có thể tiếp tục giữ các biến phẳng cũ trong variable group. Nếu chuyển toàn bộ sang chuẩn mới (`ECM_*__*`), pipeline vẫn hoạt động vì script sẽ lấy giá trị fallback tương ứng. Nếu cả hai tên cùng để trống, job Deploy sẽ dừng và báo thiếu biến.

Ngoài các biến trên, hãy đảm bảo variable group còn chứa:

- `HARBOR_USER`, `HARBOR_PASS` – tài khoản push/pull image.
- Các thông tin khác mà `docker compose` yêu cầu (ví dụ `MINIO_BUCKET`, `ECM_SERVICE_URL`, ... nếu bạn sử dụng trong file compose). Những biến này có thể tiếp tục export ngay trong script hoặc thêm trực tiếp vào variable group.

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
