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
  [switch]$Idempotent,

  # NEW: only build when you explicitly ask for it
  [switch]$ForceBuild
)

$ErrorActionPreference = "Stop"
$repoRoot = (Resolve-Path "$PSScriptRoot/../../").Path
Set-Location $repoRoot

$settingsPath = Join-Path $repoRoot "ecm.settings.json"
if (!(Test-Path $settingsPath)) { throw "❌ Cannot find ecm.settings.json at $settingsPath" }
$settings = Get-Content $settingsPath -Raw | ConvertFrom-Json

$startup = $settings.StartupProject
$map = @{
  "iam"      = @{ Key="iam";      Ctx = $settings.Contexts.iam.Context;      Proj = $settings.Contexts.iam.Project;      Root = "src/Modules/IAM" };
  "document" = @{ Key="document"; Ctx = $settings.Contexts.document.Context; Proj = $settings.Contexts.document.Project; Root = "src/Modules/Document" };
  "file"     = @{ Key="file";     Ctx = $settings.Contexts.file.Context;     Proj = $settings.Contexts.file.Project;    Root = "src/Modules/File" };
}

function Get-Targets($module) {
  if ($module -eq "all") { return @($map.iam, $map.document, $map.file) }
  if (-not $map.ContainsKey($module)) { throw "Invalid module. Use: iam | document | file | all" }
  return @($map[$module])
}

# ⏭️ Skip auto-build by default; build only when -ForceBuild is passed
if ($ForceBuild) {
  Write-Host "🔧 Building startup project ($Configuration) ..."
  dotnet build $startup -c $Configuration | Out-Null
} else {
  Write-Host "⏩ Skipping build (using --no-build for EF commands)."
}

switch ($Action) {
  "list" {
    dotnet ef dbcontext list --startup-project $startup --no-build
  }

  "add" {
    if (-not $Module -or $Module -eq "all") { throw "❌ 'add' must target a single module. Use -Module iam|document|file" }
    if (-not $Name) { throw "❌ Missing -Name (migration name)" }
    $t = $map[$Module]
    dotnet ef migrations add $Name `
      --context $t.Ctx `
      --project $t.Proj `
      --startup-project $startup `
      --configuration $Configuration `
      --output-dir $OutputDir `
      --no-build
  }

  "update" {
    foreach ($t in (Get-Targets $Module)) {
      Write-Host "==> Applying migrations for: $($t.Key)"
      dotnet ef database update `
        --context $t.Ctx `
        --project $t.Proj `
        --startup-project $startup `
        --configuration $Configuration `
        --no-build
    }
    Write-Host "✅ Database update completed."
  }

  "rollback" {
    if (-not $Name) { throw "❌ Missing -Name (target migration, e.g., 0 or 20251017_AddX)" }
    foreach ($t in (Get-Targets $Module)) {
      Write-Host "==> Rolling back: $($t.Key) → $Name"
      dotnet ef database update $Name `
        --context $t.Ctx `
        --project $t.Proj `
        --startup-project $startup `
        --configuration $Configuration `
        --no-build
    }
    Write-Host "✅ Rollback completed."
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
                "-o", $out,
                "--no-build")

      if ($From) { $cmd = @($From) + $cmd }
      if ($To)   { $cmd = $cmd + @($To) }
      if ($Idempotent) { $cmd += "--idempotent" }

      Write-Host "==> Generating SQL script for: $($t.Key) → $out"
      dotnet ef @cmd
    }
    Write-Host "✅ SQL script(s) generated in: $outDir"
  }

  "miglist" {
    foreach ($t in (Get-Targets $Module)) {
      Write-Host "`n=== Available migrations for: $($t.Key) ==="
      dotnet ef migrations list `
        --context $t.Ctx `
        --project $t.Proj `
        --startup-project $startup `
        --configuration $Configuration `
        --no-build
    }
  }

  "diag" {
    foreach ($t in (Get-Targets $Module)) {
      Write-Host "`n=== DIAGNOSTICS for: $($t.Key) (verbose EF output) ==="
      dotnet ef migrations list `
        --context $t.Ctx `
        --project $t.Proj `
        --startup-project $startup `
        --configuration $Configuration `
        -v `
        --no-build
    }
    Write-Host "`n💡 Tip: Check 'Migrations assembly', 'Startup project', and 'DbContext' in the verbose logs above."
  }

  "scan" {
    foreach ($t in (Get-Targets $Module)) {
      Write-Host "`n=== Scanning source files for [Migration(...)] in: $($t.Key) ==="
      $root = $t.Root
      if (!(Test-Path $root)) { Write-Host "  (skip) Folder not found: $root"; continue }
      Get-ChildItem -Path $root -Recurse -Include *.cs | ForEach-Object {
        $lines = Select-String -Path $_.FullName -Pattern '^\s*\[Migration\(".*' | Select-Object LineNumber, Line, Path
        foreach ($l in $lines) {
          Write-Host ("  {0}:{1}: {2}" -f $l.Path, $l.LineNumber, $l.Line.Trim())
        }
      }
      Write-Host ">>> If 'scan' finds migrations but 'miglist' does not show them, they may belong to a different context or assembly."
    }
  }
}
