# Azure Pipelines – Triển khai ECM lên các máy chủ Linux

Tài liệu này mô tả pipeline Azure DevOps mới được thêm vào repo (`azure-pipelines.yml`) nhằm build, kiểm thử và triển khai ECM lên các máy chủ Linux thông qua các agent Azure DevOps.

## Kiến trúc tổng quan

Pipeline được chia thành hai stage chính:

1. **Build & Test** – chạy trên `ubuntu-latest` (Microsoft-hosted agent):
   - Cài đặt .NET SDK 9 và Node.js 20.
   - `dotnet restore`, `dotnet build` và `dotnet test` cho toàn bộ solution `ECM.sln`.
   - Build SPA của App Gateway (`src/AppGateway/ui`).
   - `dotnet publish` cho các service chính (ECM.Host, AppGateway.Api, các worker).
   - Đóng gói output, kèm manifest và script triển khai `deploy/scripts/linux-deploy.sh`, sau đó publish artifact `drop`.

2. **Deploy** – chạy lần lượt trên từng agent Linux được khai báo:
   - Tải artifact `drop`.
   - Gọi script `linux-deploy.sh` để đồng bộ mã nguồn đã publish tới thư mục đích và restart các service đã cấu hình.

Mỗi máy chủ Linux nên cài một self-hosted agent và gán vào pool riêng (ví dụ `linux-staging`, `linux-production`).

## Tham số hóa máy đích

Đầu file `azure-pipelines.yml` khai báo tham số `linuxServers` dạng object. Mỗi phần tử mô tả một đích triển khai:

```yaml
parameters:
- name: linuxServers
  type: object
  default:
    - name: staging-app
      displayName: 'Staging App Host'
      environment: 'staging'
      pool: 'linux-staging'
      deployPath: '/opt/ecm'
      services: 'ecm-host,ecm-gateway'
      preDeployScript: ''
      postDeployScript: ''
      useSudo: 'auto'
```

| Thuộc tính         | Ý nghĩa                                                                                         |
|--------------------|--------------------------------------------------------------------------------------------------|
| `name`             | Định danh duy nhất (dùng đặt tên job).                                                           |
| `displayName`      | Tên hiển thị trên giao diện pipeline.                                                           |
| `environment`      | Azure DevOps Environment phục vụ tracking deployment.                                           |
| `pool`             | Tên agent pool (mỗi pool tương ứng một/nhóm server Linux).                                      |
| `deployPath`       | Thư mục trên server để đồng bộ artifact (`/opt/ecm`, `/var/www/ecm`, ...).                       |
| `services`         | Danh sách dịch vụ systemd cần restart sau khi copy (phân tách bằng dấu phẩy).                    |
| `preDeployScript`  | Lệnh shell chạy trước khi copy (ví dụ backup, thông báo maintenance).                            |
| `postDeployScript` | Lệnh shell chạy sau khi copy (ví dụ migrate DB, warmup).                                         |
| `useSudo`          | `auto` (mặc định), `always` hoặc `never` để điều khiển việc sử dụng `sudo` trong script deploy.  |

Khi cần thêm/loại bỏ server, cập nhật danh sách trên (hoặc override tham số khi queue build).

## Script triển khai trên agent Linux

`deploy/scripts/linux-deploy.sh` được copy vào artifact và chạy trực tiếp trên agent (vì agent nằm ngay trên server đích):

- Nhận đường dẫn artifact (`$(Pipeline.Workspace)/drop`).
- Dựa trên biến môi trường `ECM_DEPLOY_ROOT` để xác định thư mục triển khai.
- Đồng bộ file bằng `rsync` (nếu có) hoặc `cp -a` + `rm -rf`.
- Thực thi `ECM_DEPLOY_PRE_SCRIPT`/`ECM_DEPLOY_POST_SCRIPT` nếu có.
- Restart các service trong `ECM_DEPLOY_SERVICES` bằng `systemctl`.

Nếu muốn chạy mà không cần quyền `sudo`, đặt `useSudo: 'never'` trong tham số server. Nếu muốn luôn dùng `sudo`, đặt `useSudo: 'always'`.

## Cấu hình self-hosted agent Linux (tóm tắt)

1. Tạo **Agent Pool** mới trên Azure DevOps (ví dụ `linux-staging`).
2. Tại mỗi server Linux:
   - Cài đặt .NET runtime cần thiết (nếu service yêu cầu), Node (tùy nhu cầu), và agent Azure DevOps theo hướng dẫn của Microsoft.
   - Đăng ký agent vào pool tương ứng.
   - Đảm bảo agent chạy với quyền đủ để ghi vào `deployPath` và restart service (có thể thêm user vào `sudoers`).
3. Khởi tạo các service systemd chạy ECM (ví dụ `ecm-host.service`, `ecm-gateway.service`) để pipeline có thể restart.

## Tùy biến bổ sung

- **Trigger**: mặc định pipeline chạy khi push lên `main`. Nếu cần branch khác, cập nhật block `trigger` hoặc tạo pipeline mới tham chiếu file này.
- **Project publish**: biến `publishProjects` dùng để liệt kê `.csproj` cần `dotnet publish`. Thêm/bớt project bằng cách chỉnh sửa danh sách.
- **Artifact manifest**: `manifest.json` chứa `buildNumber`, `sourceVersion`, ... có thể mở rộng để lưu thông tin versioning khác.

## Quy trình triển khai mẫu

1. Push code lên `main` → pipeline auto chạy stage Build.
2. Artifact `drop` được tạo, chứa cấu trúc:
   ```
   drop/
     AppGateway.Api/
     ECM.Host/
     Notify.Worker/
     OutboxDispatcher.Worker/
     SearchIndexer.Worker/
     AppGateway.Api/wwwroot/
     manifest.json
     tools/linux-deploy.sh
   ```
3. Stage Deploy chạy trên từng agent Linux và cập nhật code tại `deployPath`.
4. Service được restart theo cấu hình (`ecm-host`, `ecm-gateway`, ...).

Theo dõi tiến trình/nhật ký tại tab **Environments** hoặc trực tiếp trong mỗi job của stage Deploy.
