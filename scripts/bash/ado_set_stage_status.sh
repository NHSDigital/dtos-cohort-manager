#!/bin/bash

set -e

# Check for input
if [[ -z "$1" ]]; then
    echo "Error: No JSON input provided." >&2
    exit 1
fi

stage_statuses_json=$1

failed_stages=""
skipped_stages=""

while IFS= read -r stage; do
    status=$(echo "$stage_statuses_json" | jq -r --arg stage_key "$stage" '.[$stage_key]')
    echo "$stage status: $status"

    if [[ "$status" == "" ]] || [[ "$status" == "null" ]]; then
        [[ -n "$skipped_stages" ]] && skipped_stages+=", "
        skipped_stages+="$stage"
    elif [[ "$status" != "succeeded" ]]; then
        [[ -n "$failed_stages" ]] && failed_stages+=", "
        failed_stages+="$stage"
    fi
done < <(echo "$stage_statuses_json" | jq -r 'keys[]')

echo "Skipped stages: $skipped_stages"
echo "Failed stages: $failed_stages"


# Set the final output variables
if [[ -z "$failed_stages" ]]; then
    echo "##vso[task.setvariable variable=status;isOutput=true]succeeded"
else
    echo "##vso[task.setvariable variable=status;isOutput=true]failed"
    echo "##vso[task.setvariable variable=failedStages;isOutput=true]$failed_stages"
fi

if [[ -n "$skipped_stages" ]]; then
    echo "##vso[task.setvariable variable=skippedStages;isOutput=true]$skipped_stages"
else
    echo "##vso[task.setvariable variable=skippedStages;isOutput=true]None"
fi
