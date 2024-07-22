#!/bin/bash

declare -A docker_functions_map=(["CohortDistributionServices/AddCohortDistributionData"]="add-cohort-distribution-data")
for dir in ${{ steps.changed-files.outputs.all_changed_files }}; do
echo "${docker_functions_map[$dir]}"
done