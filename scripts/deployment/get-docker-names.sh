#!/bin/bash

declare -A docker_functions_map=(
    ["CaasIntegration/processCaasFile"]="process-caas-file"
    ["CaasIntegration/receiveCaasFile"]="receive-caas-file"
    ["ExceptionHandling/CreateException"]="create-exception"
    ["CohortDistributionServices/AddCohortDistributionData"]="add-cohort-distribution-data"
    ["CohortDistributionServices/CreateCohortDistribution"]="create-cohort-distribution" #inconsistant file name for the function (should be create-cohort-distribution-data )
    #["CohortDistributionServices/GetCohortDistributionParticipants"]="get-cohort-distribution-participants" # will be used in the future
    ["CohortDistributionServices/RemoveCohortDistributionData"]="remove-cohort-distribution-data"
    ["CohortDistributionServices/RetrieveCohortDistribution"]="retrieve-distribution-data" #inconsistant file name for the function (should be retrieve-cohort-distribution-data )
    ["CohortDistributionServices/RetrieveParticipantData"]="retrieve-participant-data"
    ["CohortDistributionServices/ServiceProviderAllocationService"]="allocate-service-provider"
    ["CohortDistributionServices/TransformDataService"]="transform-data-service"
    ["CohortDistributionServices/ValidateCohortDistributionRecord"]="validate-cohort-distribution-record"
    ["DemographicServices/DemographicDataManagementFunction"]="demographic-data-management"
    ["DevOpsTestingService"]="devops-testing-service"
    ["ParticipantManagementServices/RemoveParticipant"]="remove-participant"
    ["ParticipantManagementServices/addParticipant"]="add-participant"
    ["ParticipantManagementServices/updateParticipant"]="update-participant"
    ["screeningDataServices/createParticipant"]="create-participant"
    ["screeningDataServices/DemographicDataService"]="demographic-data-service"
    ["screeningDataServices/GetValidationExceptions"]="get-validation-exceptions"
    ["screeningDataServices/markParticipantAsEligible"]="mark-participant-as-eligible"
    ["screeningDataServices/markParticipantAsIneligible"]="mark-participant-as-ineligible"
    ["screeningDataServices/updateParticipantDetails"]="update-participant-details"
    ["ScreeningValidationService/FileValidation"]="file-validation"
    ["ScreeningValidationService/LookupValidation"]="lookup-validation"
    ["ScreeningValidationService/StaticValidation"]="static-validation"
    ["ScreeningValidationService/RemoveValidationException"]="remove-validation-exception-data"
)

changed_functions=""

if [ -z "$CHANGED_FOLDERS" ]; then
    changed_functions="null"
    echo "No files changed"
elif [[ "$CHANGED_FOLDERS" == "*Shared*" ]]; then
    changed_functions=""
else
    echo "files changed $CHANGED_FOLDERS "
    for folder in $CHANGED_FOLDERS; do
      echo "Add this function in: ${folder} "
      echo "Add this which maps to: ${docker_functions_map[$folder]} "
      changed_functions+=" ${docker_functions_map[$folder]}"
    done
fi

# The full list of functions. Uncomment the next block when you want to redeploy all the functions.
changed_functions="process-caas-file receive-caas-file create-exception add-cohort-distribution-data \
create-cohort-distribution remove-cohort-distribution-data retrieve-distribution-data \
retrieve-participant-data allocate-service-provider transform-data-service validate-cohort-distribution-record \
demographic-data-management devops-testing-service remove-participant add-participant update-participant \
create-participant demographic-data-service get-validation-exceptions mark-participant-as-eligible \
mark-participant-as-ineligible update-participant-details file-validation lookup-validation static-validation \
remove-validation-exception"

echo "FUNC_NAMES=$changed_functions" >> "$GITHUB_OUTPUT"
