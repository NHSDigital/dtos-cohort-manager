#!/bin/bash

set -euo pipefail

# Globals set
API_URL="${API_URL:?Environment variable API_URL is required}"
REPORT_DIR="${REPORT_DIR:?Environment variable REPORT_DIR is required}"
API_KEY="${API_KEY:-}"

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

  echo "🔍 Scanning ${API_URL}"

  # Build ZAP replacer config if API_KEY is provided
  ZAP_HEADER_CFG=""

  if [[ -n "${API_KEY}" ]]; then
    echo "🔑 API key provided — adding OCP-Apim-Subscription-Key header"
    ZAP_HEADER_CFG="
      -config replacer.full_list(0).matchtype=REQ_HEADER
      -config replacer.full_list(0).matchstr=OCP-Apim-Subscription-Key
      -config replacer.full_list(0).regex=false
      -config replacer.full_list(0).replacement=${API_KEY}
      -config replacer.full_list(0).enabled=true
    "
  else
    echo "ℹ️ No API key provided — running without authentication header"
  fi

  if ! docker run --rm \
    --user root \
    -v "$(pwd)/${REPORT_DIR}:/zap/wrk" \
    ghcr.io/zaproxy/zaproxy:stable \
    zap-api-scan.py \
      -t "${API_URL}" \
      -f openapi \
      -z "${ZAP_HEADER_CFG}" \
      -j \
      -J "api.json"; then

    echo "⚠️  Warning: ZAP scan failed for ${API_URL}, continuing..."
  fi

  echo "✅ Completed. Reports saved to: ${REPORT_DIR}"

  return 0
}

main
exit 0
