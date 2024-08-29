#!/bin/bash

declare -A docker_functions_map=(
    ["CaasIntegration/ProcessCaasFile"]="process-caas-file"
    ["CaasIntegration/ReceiveCaasFile"]="receive-caas-file"
    ["ExceptionHandling/CreateException"]="create-exception"
    ["CohortDistributionServices/AddCohortDistributionData"]="add-cohort-distribution-data"
    ["CohortDistributionServices/CreateCohortDistribution"]="create-cohort-distribution" #inconsistant file name for the function (should be create-cohort-distribution-data )
    ["CohortDistributionServices/RemoveCohortDistributionData"]="remove-from-cohort-distribution-data"
    ["CohortDistributionServices/RetrieveCohortDistribution"]="retrieve-distribution-data" #inconsistant file name for the function (should be retrieve-cohort-distribution-data )
    ["CohortDistributionServices/RetrieveParticipantData"]="retrieve-participant-data"
    ["CohortDistributionServices/ServiceProviderAllocationService"]="allocate-service-provider"
    ["CohortDistributionServices/TransformDataService"]="transform-data-service"
    ["CohortDistributionServices/ValidateCohortDistributionRecord"]="validate-cohort-distribution-record"
    ["DemographicServices/DemographicDataManagement"]="demographic-data-management"
    ["DevOpsTestingService"]="devops-testing-service"
    ["ParticipantManagementServices/RemoveParticipant"]="remove-participant"
    ["ParticipantManagementServices/AddParticipant"]="add-participant"
    ["ParticipantManagementServices/UpdateParticipant"]="update-participant"
    ["ScreeningValidationService/FileValidation"]="file-validation"
    ["ScreeningValidationService/LookupValidation"]="lookup-validation"
    ["ScreeningValidationService/StaticValidation"]="static-validation"
    ["screeningDataServices/DemographicDataService"]="demographic-data-service"
    ["screeningDataServices/GetValidationExceptions"]="get-validation-exceptions"
    ["screeningDataServices/CreateParticipant"]="create-participant"
    ["screeningDataServices/markParticipantAsEligible"]="mark-participant-as-eligible"
    ["screeningDataServices/markParticipantAsIneligible"]="mark-participant-as-ineligible"
    ["screeningDataServices/UpdateParticipantDetails"]="update-participant-details"
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
      echo "Add this function in: ${docker_functions_map[$folder]}"
      changed_functions+=" ${docker_functions_map[$folder]}"
    done
fi

# The full list of functions. Uncomment the next block when you want to redeploy all the functions.
# changed_functions="process-caas-file receive-caas-file create-exception add-cohort-distribution-data \
# remove-from-cohort-distribution-data create-cohort-distribution retrieve-cohort-distribution-data allocate-service-provider \
# transform-data-service demographic-data-management remove-participant add-participant \
# update-participant file-validation lookup-validation static-validation demographic-data-service \
# get-validation-exceptions create-participant mark-participant-as-eligible mark-participant-as-ineligible \
# update-participant-details"

echo "FUNC_NAMES=$changed_functions" >> "$GITHUB_OUTPUT"
