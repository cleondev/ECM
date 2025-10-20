param(
  [Parameter(Mandatory=$true)]
  [ValidateSet("add","update","script","list","rollback","miglist","diag","scan")] [string]$Action,

  [Parameter(Mandatory=$false)]
  [ValidateSet("iam","document","file","all")] [string]$Module,

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

$settingsPath = Join-Path $repoRoot "ecm.settings.json"
if (!(Test-Path $settingsPath)) { throw "Không tìm thấy ecm.settings.json tại $settingsPath" }
$settings = Get-Content $settingsPath -Raw | ConvertFrom-Json

$startup = $settings.StartupProject
$map = @{
  "iam"      = @{ Key="iam";      Ctx = $settings.Contexts.iam.Context;      Proj = $settings.Contexts.iam.Project;      Root = "src/Modules/IAM" };
  "document" = @{ Key="document"; Ctx = $settings.Contexts.document.Context; Proj = $settings.Contexts.document.Project; Root = "src/Modules/Document" };
  "file"     = @{ Key="file";     Ctx = $settings.Contexts.file.Context;     Proj = $settings.Contexts.file.Project;    Root = "src/Modules/File" };
}

function Get-Targets($module) {
  if ($module -eq "all") { return @($map.iam, $map.document, $map.file) }
  if (-not $map.ContainsKey($module)) { throw "Module không hợp lệ. Dùng: iam|document|file|all" }
  return @($map[$module])
}

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

  "diag" {
    foreach ($t in (Get-Targets $Module)) {
      Write-Host "`n=== DIAG: $($t.Key) (verbose EF) ==="
      dotnet ef migrations list `
        --context $t.Ctx `
        --project $t.Proj `
        --startup-project $startup `
        --configuration $Configuration -v
    }
    Write-Host "`nTip: kiểm tra 'Migrations assembly', 'Startup project' và 'DbContext' trong log verbose."
  }

  "scan" {
    foreach ($t in (Get-Targets $Module)) {
      Write-Host "`n=== SCAN source for [Migration(...)] in $($t.Key) ==="
      $root = $t.Root
      if (!(Test-Path $root)) { Write-Host "  (skip) Không tìm thấy $root"; continue }
      Get-ChildItem -Path $root -Recurse -Include *.cs | ForEach-Object {
        $lines = Select-String -Path $_.FullName -Pattern '^\s*\[Migration\(".*' | Select-Object LineNumber, Line, Path
        foreach ($l in $lines) {
          Write-Host ("  {0}:{1}: {2}" -f $l.Path, $l.LineNumber, $l.Line.Trim())
        }
      }
      Write-Host ">>> Nếu SCAN thấy nhiều migration nhưng 'miglist' không thấy, có thể migration thuộc context/assembly khác."
    }
  }
}
