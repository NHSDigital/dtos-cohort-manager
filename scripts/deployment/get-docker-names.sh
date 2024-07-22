#!/bin/bash

declare -A docker_functions_map=(
    ["CohortDistributionServices/AddCohortDistributionData"]="add-cohort-distribution-data"
    ["CohortDistributionServices/ServiceProviderAllocationService/AllocateServiceProviderToParticipantByService"]="allocate-service-provider"
)

changed_functions=""

for dir in $CHANGED_FOLDERS; do
changed_functions+="${docker_functions_map[$dir]}"
done

echo "printing"
echo "changed folders: $CHANGED_FOLDERS"
# echo "${docker_functions_map[$CHANGED_FOLDERS]}"
echo "FUNC_NAMES=$changed_functions" >> "$GITHUB_OUTPUT" 