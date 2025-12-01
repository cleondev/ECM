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
DB_USER=${DB_USER:-ecm}
DB_PASSWORD=${DB_PASSWORD:-ecm}
DB_NAME_PREFIX=${DB_NAME_PREFIX:-ecm}

database_name_for() {
  local schema=${1:-}
  local override_var="DB_NAME_${schema^^}"
  local override=${!override_var:-}

  if [[ -n "${override}" ]]; then
    printf '%s' "${override}"
  else
    printf '%s_%s' "${DB_NAME_PREFIX}" "${schema}"
  fi
}

connection_string_for() {
  local schema=${1:-}
  local database
  database=$(database_name_for "${schema}")

  printf 'Host=%s;Port=%s;Database=%s;Username=%s;Password=%s' \
    "${DB_HOST}" \
    "${DB_PORT}" \
    "${database}" \
    "${DB_USER}" \
    "${DB_PASSWORD}"
}

module_mappings=(
  IAM:iam
  Document:doc
  File:doc
  Workflow:wf
  Search:search
  Ocr:ocr
  Operations:ops
  Webhook:webhook
)

for mapping in "${module_mappings[@]}"; do
  module="${mapping%%:*}"
  schema="${mapping##*:}"
  export "ConnectionStrings__${module}"="$(connection_string_for "${schema}")"
done

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
  - ConnectionStrings__<Module> -> ${DB_HOST}:${DB_PORT}/${DB_NAME_PREFIX}_<schema>
  - FileStorage__ServiceUrl      -> ${FileStorage__ServiceUrl:-http://localhost:9000}
  - Kafka__BootstrapServers      -> ${Kafka__BootstrapServers:-localhost:9092}
  - Services__Ecm                -> ${Services__Ecm:-http://localhost:8080}
  - Workflow__Camunda__BaseUrl   -> ${Workflow__Camunda__BaseUrl:-http://localhost:8080/engine-rest}

Tuỳ chỉnh bằng cách đặt sẵn các biến DB_HOST, DB_NAME_PREFIX, DB_NAME_<SCHEMA>, DB_USER... trước khi source script.
SETTINGS
