#!/bin/bash

set -euo pipefail

RUN_ID="$1"
ORG_URL="$2"
PROJECT="$3"
TIMEOUT_SECONDS="${4:-900}"  # Default to 15 minutes if not provided

SLEEP_TIME=15

echo "Waiting for Azure DevOps pipeline run $RUN_ID to complete..."

START_TIME=$(date +%s)

while true; do
  PIPELINE_JSON=$(az pipelines runs show \
    --id "$RUN_ID" \
    --org "$ORG_URL" \
    --project "$PROJECT" \
    --output json)
  STATUS=$(echo "$PIPELINE_JSON" | jq -r '.status')
  RESULT=$(echo "$PIPELINE_JSON" | jq -r '.result')

  if [[ "$STATUS" == "completed" ]]; then
    if [[ "$RESULT" == "succeeded" ]]; then
      echo "Status: $STATUS. Pipeline run $RUN_ID succeeded."
      exit 0
    else
      echo "Status: $STATUS. Pipeline run $RUN_ID failed with result: $RESULT"
      exit 1
    fi
  fi

  CURRENT_TIME=$(date +%s)
  ELAPSED=$((CURRENT_TIME - START_TIME))
  if (( ELAPSED > TIMEOUT_SECONDS )); then
    echo "ERROR: Timeout of ${TIMEOUT_SECONDS}s reached while waiting for pipeline run $RUN_ID."
    exit 2
  fi

  echo "Status: $STATUS (Elapsed: ${ELAPSED}s)"
  sleep "$SLEEP_TIME"
done
