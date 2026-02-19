#!/bin/bash

set -euo pipefail

usage() {
  echo "Usage: $0 <functionapps_file> <report_dir>"
  echo
  echo "  <functionapps_file>  Path to file containing one hostname per line"
  echo "  <report_dir>         Directory to write ZAP reports into"
  exit 2
}

# Globals set by validate_args
FILE=""
REPORT_DIR=""

validate_args() {
  # Require exactly two arguments
  if [[ $# -ne 2 ]]; then
    usage
  fi

  local file_arg="$1"
  local report_dir_arg="$2"

  # Validate input file exists and is a regular file
  if [[ ! -f "$file_arg" ]]; then
    echo "❌ Error: apps list file '$file_arg' does not exist or is not a regular file."
    exit 1
  fi

  # Assign to globals only after validation passes
  FILE="$file_arg"
  REPORT_DIR="$report_dir_arg"
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

  # Process each app
  while IFS= read -r app; do
    # Skip empty lines & comments
    [[ -z "$app" || "$app" =~ ^[[:space:]]*# ]] && continue
    app="$(echo "$app" | xargs)"  # trim

    echo "🔍 Scanning https://${app}"

    if ! docker run --rm \
      --user root \
      -v "$(pwd)/${REPORT_DIR}:/zap/wrk" \
      ghcr.io/zaproxy/zaproxy:stable \
      zap-baseline.py \
        -t "https://${app}" \
        -j \
        -J "${app}.json"; then

      echo "⚠️  Warning: ZAP scan failed for ${app}, continuing..."
    fi
  done < "$FILE"

  echo "✅ Completed. Reports saved to: ${REPORT_DIR}"
}

# ---- Order matters: validate first, then main ----
validate_args "$@"
main
exit 0
