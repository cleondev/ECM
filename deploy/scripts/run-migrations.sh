#!/usr/bin/env bash

set -euo pipefail

SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
ROOT_DIR="$( cd "${SCRIPT_DIR}/../.." && pwd )"
STARTUP_PROJECT="${ROOT_DIR}/src/ECM/ECM.Host/ECM.Host.csproj"

PROJECTS=(
  "${ROOT_DIR}/src/Modules/IAM/ECM.IAM.csproj:IamDbContext"
  "${ROOT_DIR}/src/Modules/Document/ECM.Document.csproj:DocumentDbContext"
  "${ROOT_DIR}/src/Modules/File/ECM.File.csproj:FileDbContext"
)

if ! command -v dotnet >/dev/null 2>&1; then
  echo "dotnet CLI is required but was not found in PATH." >&2
  exit 1
fi

TOOL_MANIFEST="${ROOT_DIR}/.config/dotnet-tools.json"

if [ -f "${TOOL_MANIFEST}" ]; then
  echo "Restoring local dotnet tools..."
  dotnet tool restore --tool-manifest "${TOOL_MANIFEST}"
elif ! command -v dotnet-ef >/dev/null 2>&1; then
  echo "dotnet-ef CLI tool is required but was not found. Install it globally (dotnet tool install --global dotnet-ef) or add a tool manifest." >&2
  exit 1
fi

dotnet restore "${ROOT_DIR}/ECM.sln"

echo "Building startup project to ensure migrations are up to date..."
dotnet build "${STARTUP_PROJECT}"

for entry in "${PROJECTS[@]}"; do
  IFS=":" read -r project context <<<"${entry}"
  migrations_dir="$( dirname "${project}" )/Infrastructure/Migrations"

  if [ ! -d "${migrations_dir}" ] || ! find "${migrations_dir}" -maxdepth 1 -type f -name "*.cs" ! -name "*ModelSnapshot.cs" | grep -q .; then
    echo "Skipping ${context} (${project}) - no migrations found."
    echo
    continue
  fi

  echo "Applying migrations for ${context} (${project})"
  dotnet ef database update \
    --project "${project}" \
    --startup-project "${STARTUP_PROJECT}" \
    --context "${context}"
  echo
done

echo "All module migrations have been applied."
