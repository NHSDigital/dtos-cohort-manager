#!/bin/bash

set -eo pipefail

BASE_PATHS_FILE="base-image-paths.txt"
build_base_image=true


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


#code for base image folders and files set build_base_image to true if output from source_changes are in text file that has a list of folders where the shared code lives

echo "BASE_IMAGE_CHANGE=${build_base_image}" >> "${GITHUB_OUTPUT}"
