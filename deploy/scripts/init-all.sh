#!/usr/bin/env bash
#
# Thiết lập toàn bộ biến môi trường cần thiết cho môi trường DEV/local.
# Dựa trên cấu hình mặc định của deploy/compose.yml.
#
# Sử dụng: source deploy/scripts/init-all.sh
#         (đừng chạy trực tiếp vì export sẽ không giữ lại ở shell hiện tại)

set -euo pipefail

DB_HOST=${DB_HOST:-localhost}
DB_PORT=${DB_PORT:-5432}
DB_NAME=${DB_NAME:-ecm}
DB_USER=${DB_USER:-ecm}
DB_PASSWORD=${DB_PASSWORD:-ecm}

base_connection_string() {
  local schema=${1:-}

  if [[ -n "${schema}" ]]; then
    printf 'Host=%s;Port=%s;Database=%s;Username=%s;Password=%s;Search Path=%s' "${DB_HOST}" "${DB_PORT}" "${DB_NAME}" "${DB_USER}" "${DB_PASSWORD}" "${schema}"
  else
    printf 'Host=%s;Port=%s;Database=%s;Username=%s;Password=%s' "${DB_HOST}" "${DB_PORT}" "${DB_NAME}" "${DB_USER}" "${DB_PASSWORD}"
  fi
}

#
# Các module sử dụng DatabaseConfigurationExtensions để dò tìm connection string theo thứ tự:
#   1. Tên module (AccessControl, Document, ...)
#   2. Tên schema tương ứng (iam, doc, ...)
#   3. Giá trị mặc định "postgres"
# Vì vậy chúng ta export ConnectionStrings cho cả module lẫn schema để khớp với appsettings.json.
#
declare -A MODULE_SCHEMAS=(
  [AccessControl]=iam
  [Document]=doc
  [File]=doc
  [Workflow]=wf
  [Search]=search
  [Ocr]=ocr
  [Operations]=ops
)

for module in "${!MODULE_SCHEMAS[@]}"; do
  schema=${MODULE_SCHEMAS["${module}"]}
  export "ConnectionStrings__${module}"="$(base_connection_string "${schema}")"
  export "ConnectionStrings__${schema}"="$(base_connection_string "${schema}")"
  export "Database__Schemas__${module}"="${schema}"
done

export ConnectionStrings__postgres="$(base_connection_string)"
export ConnectionStrings__Outbox="$(base_connection_string)"
export ConnectionStrings__Ops="$(base_connection_string "ops")"

export FileStorage__BucketName=${FileStorage__BucketName:-ecm-files}
export FileStorage__ServiceUrl=${FileStorage__ServiceUrl:-http://localhost:9000}
export FileStorage__AccessKeyId=${FileStorage__AccessKeyId:-minio}
export FileStorage__SecretAccessKey=${FileStorage__SecretAccessKey:-miniominio}
export FileStorage__Region=${FileStorage__Region:-us-east-1}
export FileStorage__ForcePathStyle=${FileStorage__ForcePathStyle:-true}

export Kafka__BootstrapServers=${Kafka__BootstrapServers:-localhost:9092}

export Services__Ecm=${Services__Ecm:-http://localhost:8080}

export Workflow__Camunda__BaseUrl=${Workflow__Camunda__BaseUrl:-http://localhost:8080/engine-rest}
export Workflow__Camunda__TenantId=${Workflow__Camunda__TenantId:-default}

cat <<SETTINGS
Đã export các biến môi trường chính cho ECM:
  - ConnectionStrings__<Module> / ConnectionStrings__<schema> -> ${DB_HOST}:${DB_PORT}/${DB_NAME}
  - ConnectionStrings__postgres / Outbox / Ops                -> ${DB_HOST}:${DB_PORT}/${DB_NAME}
  - FileStorage__ServiceUrl                                   -> ${FileStorage__ServiceUrl:-http://localhost:9000}
  - Kafka__BootstrapServers                                   -> ${Kafka__BootstrapServers:-localhost:9092}
  - Services__Ecm                                             -> ${Services__Ecm:-http://localhost:8080}
  - Workflow__Camunda__BaseUrl                                -> ${Workflow__Camunda__BaseUrl:-http://localhost:8080/engine-rest}

Tuỳ chỉnh bằng cách đặt sẵn các biến DB_HOST, DB_USER... trước khi source script.
SETTINGS
