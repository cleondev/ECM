param(
  [Parameter(Mandatory=$true)][ValidateSet("add","update","script","list","rollback","miglist")] [string]$Action,
  [Parameter(Mandatory=$false)][ValidateSet("iam","document","file","all")] [string]$Module,
  [string]$Name = "",
  [string]$Configuration = "Debug",
  [string]$OutputDir = "Infrastructure/Persistence/Migrations",
  [string]$From = "",
  [string]$To = "",
  [switch]$Idempotent
)

$ErrorActionPreference = "Stop"
$repoRoot = (Resolve-Path "$PSScriptRoot/../../").Path
Set-Location $repoRoot

# Load settings
$settingsPath = Join-Path $repoRoot "ecm.settings.json"
if (!(Test-Path $settingsPath)) { throw "Không tìm thấy ecm.settings.json tại $settingsPath" }
$settings = Get-Content $settingsPath -Raw | ConvertFrom-Json

$startup = $settings.StartupProject
$map = @{
  "iam"      = @{ Key="iam";      Ctx = $settings.Contexts.iam.Context;      Proj = $settings.Contexts.iam.Project };
  "document" = @{ Key="document"; Ctx = $settings.Contexts.document.Context; Proj = $settings.Contexts.document.Project };
  "file"     = @{ Key="file";     Ctx = $settings.Contexts.file.Context;     Proj = $settings.Contexts.file.Project };
}

function Get-Targets($module) {
  if ($module -eq "all") { return @($map.iam, $map.document, $map.file) }
  if (-not $map.ContainsKey($module)) { throw "Module không hợp lệ. Dùng: iam|document|file|all" }
  return @($map[$module])
}

# Build startup once
dotnet build $startup -c $Configuration | Out-Null

switch ($Action) {
  "list"   {
    dotnet ef dbcontext list --startup-project $startup
  }

  "add"    {
    if (-not $Module -or $Module -eq "all") { throw "❌ 'add' chỉ áp dụng cho 1 module. Dùng -Module iam|document|file" }
    if (-not $Name) { throw "❌ Thiếu -Name" }
    $t = $map[$Module]
    dotnet ef migrations add $Name `
      --context $t.Ctx `
      --project $t.Proj `
      --startup-project $startup `
      --configuration $Configuration `
      --output-dir $OutputDir
  }

  "update" {
    foreach ($t in (Get-Targets $Module)) {
      Write-Host "==> Updating: $($t.Key)"
      dotnet ef database update `
        --context $t.Ctx `
        --project $t.Proj `
        --startup-project $startup `
        --configuration $Configuration
    }
    Write-Host "✅ Update done."
  }

  "rollback" {
    if (-not $Name) { throw "❌ Thiếu -Name (migration target, ví dụ 0 hoặc 20251017_AddX)" }
    foreach ($t in (Get-Targets $Module)) {
      Write-Host "==> Rollback: $($t.Key) → $Name"
      dotnet ef database update $Name `
        --context $t.Ctx `
        --project $t.Proj `
        --startup-project $startup `
        --configuration $Configuration
    }
    Write-Host "✅ Rollback done."
  }

  "script" {
    $outDir = "deploy/artifacts"
    New-Item -ItemType Directory -Force -Path $outDir | Out-Null
    foreach ($t in (Get-Targets $Module)) {
      $out = Join-Path $outDir ("ef-{0}.sql" -f $t.Key)
      $cmd = @("migrations","script",
                "--context", $t.Ctx,
                "--project", $t.Proj,
                "--startup-project", $startup,
                "--configuration", $Configuration,
                "-o", $out)

      if ($From) { $cmd = @($From) + $cmd }
      if ($To)   { $cmd = $cmd + @($To) }
      if ($Idempotent) { $cmd += "--idempotent" }

      Write-Host "==> Generating SQL for: $($t.Key) → $out"
      dotnet ef @cmd
    }
    Write-Host "✅ Script(s) generated at: $outDir"
  }

  "miglist" {
    foreach ($t in (Get-Targets $Module)) {
      Write-Host "`n=== Migrations for: $($t.Key) ==="
      dotnet ef migrations list `
        --context $t.Ctx `
        --project $t.Proj `
        --startup-project $startup `
        --configuration $Configuration
    }
  }
}
