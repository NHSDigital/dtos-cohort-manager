#!/usr/bin/env bash
set -euo pipefail

# Pre-pull only Azure Functions base as amd64 (lacks ARM64 variant)
echo "Pre-pulling Azure Functions base image as amd64..."
podman pull --arch amd64 mcr.microsoft.com/azure-functions/dotnet-isolated:4-dotnet-isolated8.0

# Parse compose files and build images
for compose_file in "$@"; do
  if [[ ! -f "$compose_file" ]]; then
    echo "Warning: $compose_file not found, skipping"
    continue
  fi
  
  echo "Processing $compose_file..."
  
  # Extract image, context, dockerfile info using awk
  awk '
    /^  [a-zA-Z0-9_-]+:/ {
      if (img && ctx && df) {
        print img "|" ctx "|" df
      }
      img=""; ctx=""; df=""
      gsub(/:$/, "", $1)
      img = $1
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
      if (img && ctx && df) {
        print img "|" ctx "|" df
      }
    }
  ' "$compose_file" | while IFS='|' read -r image context dockerfile; do
    
    if [[ -z "$image" || -z "$context" || -z "$dockerfile" ]]; then
      continue
    fi
    
    dockerfile_path="$context/$dockerfile"
    if [[ ! -f "$dockerfile_path" ]]; then
      echo "Warning: Dockerfile not found at $dockerfile_path, skipping $image"
      continue
    fi
    
    # Check if Dockerfile uses Azure Functions base
    if grep -q "FROM.*mcr\.microsoft\.com/azure-functions/dotnet-isolated" "$dockerfile_path"; then
      echo "Building $image as amd64 (uses Azure Functions base)"
      podman build --arch amd64 -t "cohort-manager-$image" -f "$dockerfile_path" "$context"
    else
      echo "Building $image natively"
      podman build -t "cohort-manager-$image" -f "$dockerfile_path" "$context"
    fi
  done
done

echo "Build complete!"