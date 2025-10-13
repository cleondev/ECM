#!/bin/sh
set -euo pipefail

BROKER="${KAFKA_BROKER:-localhost:9092}"
PARTITIONS="${KAFKA_TOPIC_PARTITIONS:-3}"
REPLICAS="${KAFKA_TOPIC_REPLICAS:-1}"
RPK_BIN="${RPK_BIN:-rpk}"

TOPICS="
iam.events
document.events
version.events
workflow.events
signature.events
ocr.events
search.events
audit.events
retention.events
"

echo "[topics-init] waiting for broker ${BROKER} to be reachable..."
until ${RPK_BIN} cluster info --brokers "${BROKER}" >/dev/null 2>&1; do
  sleep 2
done

echo "[topics-init] ensuring required topics exist"
for topic in ${TOPICS}; do
  if ${RPK_BIN} topic describe "${topic}" --brokers "${BROKER}" >/dev/null 2>&1; then
    echo "[topics-init] topic '${topic}' already exists"
    continue
  fi

  echo "[topics-init] creating topic '${topic}' (partitions=${PARTITIONS}, replicas=${REPLICAS})"
  ${RPK_BIN} topic create "${topic}" \
    --brokers "${BROKER}" \
    --partitions "${PARTITIONS}" \
    --replicas "${REPLICAS}"
done

echo "[topics-init] topic initialization complete"
