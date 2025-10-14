# Thiết lập toàn bộ biến môi trường cần thiết cho môi trường DEV/local.
# Dựa trên cấu hình mặc định của deploy/compose.yml.
#
# Sử dụng: .\deploy\scripts\init-all.ps1 (từ PowerShell)

if (-not $env:DB_HOST) { $env:DB_HOST = "localhost" }
if (-not $env:DB_PORT) { $env:DB_PORT = "5432" }
if (-not $env:DB_NAME) { $env:DB_NAME = "ecm" }
if (-not $env:DB_USER) { $env:DB_USER = "ecm" }
if (-not $env:DB_PASSWORD) { $env:DB_PASSWORD = "ecm" }

$connectionString = "Host=$($env:DB_HOST);Port=$($env:DB_PORT);Database=$($env:DB_NAME);Username=$($env:DB_USER);Password=$($env:DB_PASSWORD)"
$env:ConnectionStrings__Document = $connectionString
$env:ConnectionStrings__postgres = $connectionString
$env:ConnectionStrings__Outbox = $connectionString
$env:ConnectionStrings__Ops = $connectionString

if (-not $env:FileStorage__BucketName) { $env:FileStorage__BucketName = "ecm-files" }
if (-not $env:FileStorage__ServiceUrl) { $env:FileStorage__ServiceUrl = "http://localhost:9000" }
if (-not $env:FileStorage__AccessKeyId) { $env:FileStorage__AccessKeyId = "minio" }
if (-not $env:FileStorage__SecretAccessKey) { $env:FileStorage__SecretAccessKey = "miniominio" }
if (-not $env:FileStorage__Region) { $env:FileStorage__Region = "us-east-1" }
if (-not $env:FileStorage__ForcePathStyle) { $env:FileStorage__ForcePathStyle = "true" }

if (-not $env:Kafka__BootstrapServers) { $env:Kafka__BootstrapServers = "localhost:9092" }
if (-not $env:Services__Ecm) { $env:Services__Ecm = "http://localhost:8080" }
if (-not $env:Workflow__Camunda__BaseUrl) { $env:Workflow__Camunda__BaseUrl = "http://localhost:8080/engine-rest" }
if (-not $env:Workflow__Camunda__TenantId) { $env:Workflow__Camunda__TenantId = "default" }

Write-Host "Đã export các biến môi trường chính cho ECM:" -ForegroundColor Green
Write-Host "  - ConnectionStrings__Document / postgres / Outbox / Ops -> $connectionString"
Write-Host "  - FileStorage__ServiceUrl    -> $($env:FileStorage__ServiceUrl)"
Write-Host "  - Kafka__BootstrapServers    -> $($env:Kafka__BootstrapServers)"
Write-Host "  - Services__Ecm              -> $($env:Services__Ecm)"
Write-Host "  - Workflow__Camunda__BaseUrl -> $($env:Workflow__Camunda__BaseUrl)"
Write-Host "Có thể tùy chỉnh bằng cách đặt trước các biến DB_HOST, DB_USER..." -ForegroundColor Yellow
