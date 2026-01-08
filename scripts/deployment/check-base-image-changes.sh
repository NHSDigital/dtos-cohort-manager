#!/bin/bash

set -eo pipefail

BASE_PATHS_FILE="scripts/deployment/shared-files.txt"
build_base_image=false


if [[ "${GITHUB_EVENT_NAME}" == "push" && "${GITHUB_REF}" == "refs/heads/main" ]]; then
    # Merge to main - compare merged code with main immediately prior to the merge (HEAD^), needs 'fetch-depth: 2' parameter for actions/checkout@v4
    mapfile -t source_changes < <(git diff --name-only HEAD^ -- "./" | sed -r 's#(^.*/).*$#\1#' | sort -u)
else
    # PR creation or update - compare feature branch with main, folder paths only, unique list
    git fetch origin main
    mapfile -t source_changes < <(git diff --name-only origin/main..HEAD -- "./" | sed -r 's#(^.*/).*$#\1#' | sort -u)
fi


echo -e "\nChanged source code folder(s):"
printf "  - %s\n" "${source_changes[@]}"
echo


# --- Detect base image changes ---
if [[ ! -f "${BASE_PATHS_FILE}" ]]; then
    echo "WARNING: ${BASE_PATHS_FILE} not found – base image will not be rebuilt"
else
    while IFS= read -r base_path; do
        # Skip empty lines and comments
        [[ -z "${base_path}" || "${base_path}" =~ ^# ]] && continue

        # Ensure trailing slash consistency
        base_path="${base_path%/}/"

        for changed_path in "${source_changes[@]}"; do
            if [[ "${changed_path}" == "${base_path}"* ]]; then
                echo "Base image change detected in: ${changed_path}"
                build_base_image=true
                break 2
            fi
        done
    done < "${BASE_PATHS_FILE}"
fi

echo "Base image change = ${build_base_image}"

echo "BASE_IMAGE_CHANGE=${build_base_image}" >> "${GITHUB_OUTPUT}"
