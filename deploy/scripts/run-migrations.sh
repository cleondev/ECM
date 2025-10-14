#!/usr/bin/env bash

set -euo pipefail

SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
ROOT_DIR="$( cd "${SCRIPT_DIR}/../.." && pwd )"
STARTUP_PROJECT="${ROOT_DIR}/src/ECM/ECM.Host/ECM.Host.csproj"

PROJECTS=(
  "${ROOT_DIR}/src/Modules/AccessControl/ECM.AccessControl.csproj:AccessControlDbContext"
  "${ROOT_DIR}/src/Modules/Document/ECM.Document.csproj:DocumentDbContext"
  "${ROOT_DIR}/src/Modules/File/ECM.File.csproj:FileDbContext"
)

if ! command -v dotnet >/dev/null 2>&1; then
  echo "dotnet CLI is required but was not found in PATH." >&2
  exit 1
fi

dotnet restore "${ROOT_DIR}/ECM.sln"

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
    --context "${context}" \
    --no-build
  echo
done

echo "All module migrations have been applied."
