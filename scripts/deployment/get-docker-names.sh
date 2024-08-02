#!/bin/bash

declare -A docker_functions_map=(
    ["CaasIntegration/receiveCaasFile"]="receive-caas-file"
    ["CaasIntegration/processCaasFile"]="process-caas-file"
    ["ParticipantManagementServices/addParticipant"]="add-participant"
    ["ParticipantManagementServices/RemoveParticipant"]="remove-participant"
    ["ParticipantManagementServices/updateParticipantDetails"]="update-participant"
    ["screeningDataServices/createParticipant"]="create-participant"
    ["screeningDataServices/markParticipantAsEligible"]="mark-participant-eligible"
    ["screeningDataServices/markParticipantAsIneligible"]="mark-participant-ineligible"
    ["screeningDataServices/updateParticipantDetails"]="update-participant-details"
    # ["screeningDataServices/CreateValidationException"]="create-validation-exception" # does not appear to exist.
    ["screeningDataServices/GetValidationExceptions"]="get-validation-exceptions"
    ["screeningDataServices/DemographicDataService"]="demographic-data-service"
    ["ScreeningValidationService/FileValidation"]="file-validation"
    ["ScreeningValidationService/StaticValidation"]="static-validation"
    ["ScreeningValidationService/LookupValidation"]="lookup-validation"
    ["DemographicServices/DemographicDataManagementFunction"]="demographic-data-management"
    ["CohortDistributionServices/AddCohortDistributionData"]="add-cohort-distribution-data"
    ["CohortDistributionServices/RetrieveCohortDistribution"]="retrieve-cohort-distribution-data"
    ["CohortDistributionServices/RemoveCohortDistributionData"]="remove-cohort-distribution-data"
    ["CohortDistributionServices/TransformDataService"]="transform-data"
    ["CohortDistributionServices/ServiceProviderAllocationService/AllocateServiceProviderToParticipantByService"]="allocate-service-provider"
)

changed_functions=""

if [ -z $CHANGED_FOLDERS ]; then
    changed_functions="null"
elif [[ $CHANGED_FOLDERS == "*Shared*" ]]; then
    changed_functions=""
else
    for folder in $CHANGED_FOLDERS; do
    changed_functions+=" ${docker_functions_map[$folder]}"
    done
fi

echo "FUNC_NAMES=$changed_functions" >> "$GITHUB_OUTPUT"
