# Thiết lập toàn bộ biến môi trường cần thiết cho môi trường DEV/local.
# Dựa trên cấu hình mặc định của deploy/compose.yml.
#
# Sử dụng: .\deploy\scripts\init-all.ps1 (từ PowerShell)

if (-not $env:DB_HOST) { $env:DB_HOST = "localhost" }
if (-not $env:DB_PORT) { $env:DB_PORT = "5432" }
if (-not $env:DB_USER) { $env:DB_USER = "ecm" }
if (-not $env:DB_PASSWORD) { $env:DB_PASSWORD = "ecm" }
if (-not $env:DB_NAME_PREFIX) { $env:DB_NAME_PREFIX = "ecm" }

$moduleMappings = @{
  IAM          = "iam"
  Document      = "doc"
  File          = "doc"
  Workflow      = "wf"
  Search        = "search"
  Ocr           = "ocr"
  Operations    = "ops"
  Webhook       = "webhook"
}

function Get-DatabaseName([string]$schema) {
  $overrideVar = "DB_NAME_{0}" -f $schema.ToUpper()
  $overrideItem = Get-Item -Path Env:$overrideVar -ErrorAction SilentlyContinue

  if ($null -ne $overrideItem -and $overrideItem.Value) {
    return $overrideItem.Value
  }

  return "{0}_{1}" -f $env:DB_NAME_PREFIX, $schema
}

function Get-ConnectionString([string]$schema) {
  $databaseName = Get-DatabaseName $schema
  return "Host=$($env:DB_HOST);Port=$($env:DB_PORT);Database=$databaseName;Username=$($env:DB_USER);Password=$($env:DB_PASSWORD)"
}

foreach ($entry in $moduleMappings.GetEnumerator()) {
  $module = $entry.Key
  $schema = $entry.Value
  $envName = "ConnectionStrings__{0}" -f $module
  Set-Item -Path Env:$envName -Value (Get-ConnectionString $schema)
}

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
Write-Host ("  - ConnectionStrings__<Module>  -> Host={0};Port={1};Database={2}_<schema>" -f $env:DB_HOST, $env:DB_PORT, $env:DB_NAME_PREFIX)
Write-Host "  - FileStorage__ServiceUrl    -> $($env:FileStorage__ServiceUrl)"
Write-Host "  - Kafka__BootstrapServers    -> $($env:Kafka__BootstrapServers)"
Write-Host "  - Services__Ecm              -> $($env:Services__Ecm)"
Write-Host "  - Workflow__Camunda__BaseUrl -> $($env:Workflow__Camunda__BaseUrl)"
Write-Host "Có thể tùy chỉnh bằng cách đặt trước các biến DB_HOST, DB_USER..." -ForegroundColor Yellow
