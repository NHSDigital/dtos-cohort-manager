#!/usr/bin/env bash
set -euo pipefail

COMMIT_SHA="$1"
ENVIRONMENT_NAME="$2"
DOCKER_TAG="$3"
TEST_TYPES_VALUE="$4"
BRANCH_NAME="${5:-}"    # optional

organisation="https://dev.azure.com/nhse-dtos"
project_name="dtos-cohort-manager"

param_image="dockerImageTag=${DOCKER_TAG}"
param_tests="testTypes=[${TEST_TYPES_VALUE}]"

echo "Running pipeline for environment: ${ENVIRONMENT_NAME}"
echo "dockerImageTag=${DOCKER_TAG}"
echo "testTypes=[${TEST_TYPES_VALUE}]"

if [[ -n "${BRANCH_NAME}" ]]; then
  RUN_ID=$(az pipelines run \
    --branch "${BRANCH_NAME}" \
    --commit-id "${COMMIT_SHA}" \
    --name "Deploy to Azure - Core ${ENVIRONMENT_NAME}" \
    --org "${organisation}" \
    --project "${project_name}" \
    --parameters "$param_image" "$param_tests" \
    --output tsv --query id)
else
  RUN_ID=$(az pipelines run \
    --commit-id "${COMMIT_SHA}" \
    --name "Deploy to Azure - Core ${ENVIRONMENT_NAME}" \
    --org "${organisation}" \
    --project "${project_name}" \
    --parameters "$param_image" "$param_tests" \
    --output tsv --query id)
fi

echo "Click here to view the ADO pipeline: ${organisation}/${project_name}/_build/results?buildId=${RUN_ID}"

scripts/bash/wait_ado_pipeline.sh "$RUN_ID" "${organisation}" "${project_name}" 1800