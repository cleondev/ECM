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

export ConnectionStrings__Document="Host=${DB_HOST};Port=${DB_PORT};Database=${DB_NAME};Username=${DB_USER};Password=${DB_PASSWORD}"
export ConnectionStrings__postgres="Host=${DB_HOST};Port=${DB_PORT};Database=${DB_NAME};Username=${DB_USER};Password=${DB_PASSWORD}"
export ConnectionStrings__Outbox="Host=${DB_HOST};Port=${DB_PORT};Database=${DB_NAME};Username=${DB_USER};Password=${DB_PASSWORD}"
export ConnectionStrings__Ops="Host=${DB_HOST};Port=${DB_PORT};Database=${DB_NAME};Username=${DB_USER};Password=${DB_PASSWORD}"

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
  - ConnectionStrings__Document / postgres / Outbox / Ops -> ${DB_HOST}:${DB_PORT}/${DB_NAME}
  - FileStorage__ServiceUrl    -> ${FileStorage__ServiceUrl:-http://localhost:9000}
  - Kafka__BootstrapServers    -> ${Kafka__BootstrapServers:-localhost:9092}
  - Services__Ecm              -> ${Services__Ecm:-http://localhost:8080}
  - Workflow__Camunda__BaseUrl -> ${Workflow__Camunda__BaseUrl:-http://localhost:8080/engine-rest}

Tuỳ chỉnh bằng cách đặt sẵn các biến DB_HOST, DB_USER... trước khi source script.
SETTINGS
