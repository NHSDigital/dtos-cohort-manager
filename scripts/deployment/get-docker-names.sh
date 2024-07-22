#!/bin/bash

declare -A docker_functions_map=(
    ["CohortDistributionServices/AddCohortDistributionData"]="add-cohort-distribution-data"
    ["CohortDistributionServices/ServiceProviderAllocationService/AllocateServiceProviderToParticipantByService"]="allocate-service-provider"
    ["/ParticipantManagementServices/addParticipant"]="add-participant"
)

changed_functions=""

for folder in $CHANGED_FOLDERS; do
changed_functions+=" ${docker_functions_map[$folder]}"
done

echo "printing"
echo "changed folders: $CHANGED_FOLDERS"
echo "changed functions: $changed_functions"
echo "FUNC_NAMES=$changed_functions" >> "$GITHUB_OUTPUT" 