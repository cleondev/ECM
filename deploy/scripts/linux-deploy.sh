#!/usr/bin/env bash
set -euo pipefail

if [[ $# -lt 1 ]]; then
  echo "Usage: $0 <artifact-directory>" >&2
  exit 1
fi

artifact_dir="$1"
shift || true

if [[ ! -d "$artifact_dir" ]]; then
  echo "Artifact directory '$artifact_dir' does not exist" >&2
  exit 1
fi

deploy_root="${ECM_DEPLOY_ROOT:-}"
if [[ -z "$deploy_root" ]]; then
  echo "ECM_DEPLOY_ROOT environment variable is required" >&2
  exit 1
fi

services_raw="${ECM_DEPLOY_SERVICES:-}"
pre_deploy="${ECM_DEPLOY_PRE_SCRIPT:-}"
post_deploy="${ECM_DEPLOY_POST_SCRIPT:-}"
use_sudo="${ECM_DEPLOY_USE_SUDO:-auto}"

sudo_cmd=""
if [[ "$use_sudo" == "always" ]]; then
  sudo_cmd="sudo"
elif [[ "$use_sudo" == "never" ]]; then
  sudo_cmd=""
else
  if command -v sudo >/dev/null 2>&1; then
    sudo_cmd="sudo"
  fi
fi

info() {
  echo "[deploy] $*"
}

do_run() {
  local cmd=("$@")
  if [[ -n "$sudo_cmd" ]]; then
    cmd=("$sudo_cmd" "${cmd[@]}")
  fi
  "${cmd[@]}"
}

info "Deploy root: $deploy_root"

if [[ -n "$pre_deploy" ]]; then
  info "Running pre-deploy script"
  bash -c "$pre_deploy"
fi

info "Syncing artifacts"
# Use rsync when available for efficient sync
if command -v rsync >/dev/null 2>&1; then
  do_run mkdir -p "$deploy_root"
  do_run rsync -a --delete "$artifact_dir"/ "$deploy_root"/
else
  tmp_dir="$(mktemp -d)"
  cp -a "$artifact_dir"/. "$tmp_dir"/
  do_run rm -rf "$deploy_root"
  do_run mkdir -p "$deploy_root"
  do_run cp -a "$tmp_dir"/. "$deploy_root"/
  rm -rf "$tmp_dir"
fi

if [[ -n "$post_deploy" ]]; then
  info "Running post-deploy script"
  bash -c "$post_deploy"
fi

if [[ -n "$services_raw" ]]; then
  IFS=',' read -ra services <<<"$services_raw"
  for svc in "${services[@]}"; do
    svc_name="$(echo "$svc" | xargs)"
    [[ -z "$svc_name" ]] && continue
    if command -v systemctl >/dev/null 2>&1; then
      info "Restarting service '$svc_name'"
      if [[ -n "$sudo_cmd" ]]; then
        $sudo_cmd systemctl daemon-reload || true
        $sudo_cmd systemctl restart "$svc_name"
      else
        systemctl daemon-reload || true
        systemctl restart "$svc_name"
      fi
    else
      info "systemctl not found; skipping restart for '$svc_name'"
    fi
  done
fi

info "Deployment completed"
