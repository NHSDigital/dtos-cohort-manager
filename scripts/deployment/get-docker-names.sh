#!/bin/bash

declare -A docker_functions_map=(["CohortDistributionServices/AddCohortDistributionData"]="add-cohort-distribution-data")
for dir in $CHANGED_FOLDERS; do
echo "${docker_functions_map[$dir]}"
done