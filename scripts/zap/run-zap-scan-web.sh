#!/bin/bash

set -euo pipefail

usage() {
  echo "Usage: $0 <web_url> <report_dir>"
  echo
  echo "  <web_url>      The WEB URL (without http protocol)"
  echo "  <report_dir>   Directory to write ZAP reports into"
  exit 2
}

# Globals set by validate_args
WEB_URL=""
REPORT_DIR=""

validate_args() {
  # Require exactly two arguments
  if [[ $# -ne 2 ]]; then
    usage
  fi

  WEB_URL="$1"
  REPORT_DIR="$2"
}

main() {
  # Validate Docker is installed
  if ! command -v docker >/dev/null 2>&1; then
    echo "❌ Error: Docker is not installed or not in PATH."
    exit 1
  fi

  # Prepare reports directory
  mkdir -p "$REPORT_DIR" || {
    echo "❌ Error: failed to create directory '$REPORT_DIR'"
    exit 1
  }
  chmod 777 "$REPORT_DIR" || {
    echo "❌ Error: failed to set permissions on '$REPORT_DIR'"
    exit 1
  }

  echo "🔍 Scanning ${WEB_URL}"

  if ! docker run --rm \
    --user root \
    -v "$(pwd)/${REPORT_DIR}:/zap/wrk" \
    ghcr.io/zaproxy/zaproxy:stable \
    zap-baseline.py \
      -t "${WEB_URL}" \
      -j \
      -J "web.json"; then

    echo "⚠️  Warning: ZAP scan failed for ${WEB_URL}, continuing..."
  fi

  echo "✅ Completed. Reports saved to: ${REPORT_DIR}"
}

# ---- Order matters: validate first, then main ----
validate_args "$@"
main
exit 0
