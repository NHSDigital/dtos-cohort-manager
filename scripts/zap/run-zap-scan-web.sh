#!/bin/bash

set -euo pipefail

# Globals set
WEB_URL="${WEB_URL:?Environment variable WEB_URL is required}"
REPORT_DIR="${REPORT_DIR:?Environment variable REPORT_DIR is required}"

main() {
  # Validate Docker is installed
  if ! command -v docker >/dev/null 2>&1; then
    echo "❌ Error: Docker is not installed or not in PATH." >&2
    exit 1
  fi

  # Prepare reports directory
  mkdir -p "$REPORT_DIR" || {
    echo "❌ Error: failed to create directory '$REPORT_DIR'" >&2
    exit 1
  }
  chmod 777 "$REPORT_DIR" || {
    echo "❌ Error: failed to set permissions on '$REPORT_DIR'" >&2
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

  return 0
}

main
exit 0
