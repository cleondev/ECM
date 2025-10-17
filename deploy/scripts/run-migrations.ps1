#!/usr/bin/env pwsh

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Join-Paths {
    param(
        [Parameter(Mandatory = $true, Position = 0)]
        [string] $First,
        [Parameter(ValueFromRemainingArguments = $true, Position = 1)]
        [string[]] $Rest
    )

    $path = $First
    foreach ($part in $Rest) {
        $path = Join-Path -Path $path -ChildPath $part
    }

    return $path
}

$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Definition
$rootDir = (Resolve-Path (Join-Paths $scriptDir '..' '..')).Path
$startupProject = Join-Paths $rootDir 'src' 'ECM' 'ECM.Host' 'ECM.Host.csproj'

$projects = @(
    @{ Project = (Join-Paths $rootDir 'src' 'Modules' 'IAM' 'ECM.IAM.csproj'); Context = 'IamDbContext' }
    @{ Project = (Join-Paths $rootDir 'src' 'Modules' 'Document' 'ECM.Document.csproj'); Context = 'DocumentDbContext' }
    @{ Project = (Join-Paths $rootDir 'src' 'Modules' 'File' 'ECM.File.csproj'); Context = 'FileDbContext' }
)

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Write-Error 'dotnet CLI is required but was not found in PATH.'
    exit 1
}

$toolManifestPath = Join-Paths $rootDir '.config' 'dotnet-tools.json'

if (Test-Path $toolManifestPath) {
    Write-Host 'Restoring local dotnet tools...' -ForegroundColor Cyan
    & dotnet tool restore --tool-manifest $toolManifestPath
} elseif (-not (Get-Command dotnet-ef -ErrorAction SilentlyContinue)) {
    Write-Error 'dotnet-ef CLI tool is required but was not found. Install it globally (dotnet tool install --global dotnet-ef) or add a tool manifest.'
    exit 1
}

& dotnet restore (Join-Paths $rootDir 'ECM.sln')

Write-Host 'Building startup project to ensure migrations are up to date...' -ForegroundColor Cyan
& dotnet build $startupProject

foreach ($entry in $projects) {
    $projectPath = $entry.Project
    $context = $entry.Context
    $projectDir = Split-Path -Parent $projectPath
    $migrationsDir = Join-Paths $projectDir 'Infrastructure' 'Migrations'

    if (-not (Test-Path $migrationsDir)) {
        Write-Host "Skipping $context ($projectPath) - migrations directory not found." -ForegroundColor Yellow
        Write-Host
        continue
    }

    $migrations = Get-ChildItem -Path $migrationsDir -Filter '*.cs' -File | Where-Object { $_.Name -notlike '*ModelSnapshot.cs' }
    if (-not $migrations) {
        Write-Host "Skipping $context ($projectPath) - no migrations found." -ForegroundColor Yellow
        Write-Host
        continue
    }

    Write-Host "Applying migrations for $context ($projectPath)" -ForegroundColor Cyan
    & dotnet ef database update `
        --project $projectPath `
        --startup-project $startupProject `
        --context $context
    Write-Host
}

Write-Host 'All module migrations have been applied.' -ForegroundColor Green
