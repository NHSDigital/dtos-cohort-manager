#!/usr/bin/env bash
set -euo pipefail

# Validate required tools
check_tools() {
  local missing_tools=()
  
  if ! command -v podman &> /dev/null; then
    missing_tools+=("podman")
  fi
  
  if ! command -v yq &> /dev/null; then
    echo "Warning: yq not found, falling back to basic parsing"
    return 0
  fi
  
  if [[ ${#missing_tools[@]} -ne 0 ]]; then
    echo "Error: Missing required tools: ${missing_tools[*]}" >&2
    echo "Please install the missing tools and try again." >&2
    exit 1
  fi
}

# Parse compose files using yq or fallback
parse_services() {
  local compose_file="$1"
  
  if command -v yq &> /dev/null; then
    # Use yq for robust YAML parsing
    yq eval '.services | to_entries | .[] | select(.value.build) | [.key, .value.image // ("cohort-manager-" + .key), .value.build.context, .value.build.dockerfile] | @tsv' "$compose_file" 2>/dev/null || echo ""
  else
    # Fallback to awk parsing
    awk '
      /^  [a-zA-Z0-9_-]+:/ {
        if (service && ctx && df) {
          img = service
          if (explicit_img) img = explicit_img
          print service "\t" img "\t" ctx "\t" df
        }
        service=""; ctx=""; df=""; explicit_img=""; in_build=0
        gsub(/:$/, "", $1)
        service = $1
      }
      /^    image:/ { 
        gsub(/^    image:[ ]*/, "")
        gsub(/^[ " ]+|[ " ]+$/, "")
        explicit_img = $0
      }
      /^    build:/ { in_build=1 }
      /^    [a-zA-Z]/ && !/^    build/ && in_build { in_build=0 }
      in_build && /^      context:/ {
        gsub(/^      context:[ ]*/, "")
        gsub(/^[ " ]+|[ " ]+$/, "")
        ctx = $0
      }
      in_build && /^      dockerfile:/ {
        gsub(/^      dockerfile:[ ]*/, "")
        gsub(/^[ " ]+|[ " ]+$/, "")
        df = $0
      }
      END {
        if (service && ctx && df) {
          img = service
          if (explicit_img) img = explicit_img
          print service "\t" img "\t" ctx "\t" df
        }
      }
    ' "$compose_file"
  fi
}

# Validate arguments
if [[ $# -eq 0 ]]; then
  echo "Usage: $0 <compose-file1> [compose-file2] ..." >&2
  exit 1
fi

# Check required tools
check_tools

# Pre-pull only Azure Functions base as amd64 (lacks ARM64 variant)
echo "Pre-pulling Azure Functions base image as amd64..."
if ! podman pull --arch amd64 mcr.microsoft.com/azure-functions/dotnet-isolated:4-dotnet-isolated8.0; then
  echo "Error: Failed to pull Azure Functions base image" >&2
  exit 1
fi

# Parse compose files and build images
for compose_file in "$@"; do
  if [[ ! -f "$compose_file" ]]; then
    echo "Warning: $compose_file not found, skipping" >&2
    continue
  fi
  
  echo "Processing $compose_file..."
  
  parse_services "$compose_file" | while IFS=$'\t' read -r service image context dockerfile; do
    if [[ -z "$service" || -z "$context" || -z "$dockerfile" ]]; then
      continue
    fi
    
    dockerfile_path="$context/$dockerfile"
    if [[ ! -f "$dockerfile_path" ]]; then
      echo "Warning: Dockerfile not found at $dockerfile_path, skipping $service" >&2
      continue
    fi
    
    # Check if Dockerfile uses Azure Functions base
    if grep -q "FROM.*mcr\.microsoft\.com/azure-functions/dotnet-isolated" "$dockerfile_path"; then
      echo "Building $image as amd64 (uses Azure Functions base)"
      if ! podman build --arch amd64 -t "$image" -f "$dockerfile_path" "$context"; then
        echo "Error: Failed to build $image" >&2
        exit 1
      fi
    else
      echo "Building $image natively"
      if ! podman build -t "$image" -f "$dockerfile_path" "$context"; then
        echo "Error: Failed to build $image" >&2
        exit 1
      fi
    fi
  done
  
  # Check if the while loop failed (due to pipe)
  if [[ ${PIPESTATUS[1]} -ne 0 ]]; then
    echo "Error: Build failed for $compose_file" >&2
    exit 1
  fi
done

echo "Build complete!"