#!/bin/bash

declare -A docker_functions_map=(
    ["CaasIntegration/receiveCaasFile"]="receive-caas-file"
    ["CaasIntegration/RetrieveMeshFile"]="retrieve-mesh-file"
    ["ExceptionHandling/CreateException"]="create-exception"
    ["CohortDistributionServices/AddCohortDistributionData"]="add-cohort-distribution-data"
    ["CohortDistributionServices/CreateCohortDistribution"]="create-cohort-distribution" #inconsistent file name for the function (should be create-cohort-distribution-data )
    #["CohortDistributionServices/GetCohortDistributionParticipants"]="get-cohort-distribution-participants" # will be used in the future
    ["CohortDistributionServices/RetrieveCohortDistribution"]="retrieve-cohort-distribution-data"
    #["CohortDistributionServices/RetrieveCohortReplay"]="retrieve-cohort-replay" - function removed in PR#509
    ["CohortDistributionServices/RetrieveCohortRequestAudit"]="retrieve-cohort-request-audit"
    ["CohortDistributionServices/RetrieveParticipantData"]="retrieve-participant-data"
    ["CohortDistributionServices/ServiceProviderAllocationService"]="allocate-service-provider"
    ["CohortDistributionServices/TransformDataService"]="transform-data-service"
    ["CohortDistributionServices/ValidateCohortDistributionRecord"]="validate-cohort-distribution-record"
    ["DemographicServices/DemographicDataManagementFunction"]="demographic-data-management"
    ["DemographicServices/DemographicDurableFunction"]="durable-demographic-function"
    ["ParticipantManagementServices/RemoveParticipant"]="remove-participant"
    ["ParticipantManagementServices/addParticipant"]="add-participant"
    ["ParticipantManagementServices/updateParticipant"]="update-participant"
    ["ParticipantManagementServices/GetParticipantReferenceData"]="get-participant-reference-data"
    ["ParticipantManagementServices/CheckParticipantExists"]="check-participant-exists"
    ["ParticipantManagementServices/UpdateParticipantFromScreeningProvider"]="update-participant-from-screening-provider"
    ["screeningDataServices/BsSelectGpPractice"]="bs-select-gp-practice-data-service"
    ["ParticipantManagementServices/CheckParticipantExists"]="check-participant-exists"
    ["screeningDataServices/BsSelectOutCode"]="bs-select-outcode-data-service"
    ["screeningDataServices/createParticipant"]="create-participant"
    ["screeningDataServices/CurrentPostingDataService"]="current-posting-data-service"
    ["screeningDataServices/ExceptionManagementDataService"]="exception-management-data-service"
    ["screeningDataServices/ExcludedSMULookupDataService"]="excluded-smu-data-service"
    ["screeningDataServices/GeneCodeLkpDataService"]="gene-code-lkp-data-service"
    ["screeningDataServices/HigherRiskReferralReasonLkpDataService"]="higher-risk-referral-reason-lkp-data-service"
    ["screeningDataServices/GetValidationExceptions"]="get-validation-exceptions"
    ["screeningDataServices/GPPractice"]="gppractice-data-service"
    ["screeningDataServices/LanguageCodesDataService"]="language-code-data-service"
    ["screeningDataServices/markParticipantAsEligible"]="mark-participant-as-eligible"
    ["screeningDataServices/markParticipantAsIneligible"]="mark-participant-as-ineligible"
    ["screeningDataServices/ParticipantManagementDataService"]="participant-management-data-service"
    ["screeningDataServices/ParticipantDemographicDataService"]="participant-demographic-data-service"
    ["screeningDataServices/updateParticipantDetails"]="update-participant-details"
    ["screeningDataServices/CohortDistributionDataService"]="cohort-distribution-data-service"
    ["ScreeningValidationService/FileValidation"]="file-validation"
    ["ScreeningValidationService/LookupValidation"]="lookup-validation"
    ["ScreeningValidationService/StaticValidation"]="static-validation"
    ["ScreeningValidationService/RemoveValidationException"]="remove-validation-exception-data"
)

changed_functions=""

if [ -z "$CHANGED_FOLDERS" ]; then
    changed_functions="null"
    echo "No files changed"
elif [[ "$CHANGED_FOLDERS" == *Shared* ]]; then
    echo "Shared folder changed, returning all functions"
    for key in "${!docker_functions_map[@]}"; do
        changed_functions+=" ${docker_functions_map[$key]}"
        echo "Adding in: ${docker_functions_map[$key]}"
    done
else
    echo "files changed $CHANGED_FOLDERS "
    for folder in $CHANGED_FOLDERS; do
      echo "Add this function in: ${folder} "
      echo "Add this which maps to: ${docker_functions_map[$folder]} "
      changed_functions+=" ${docker_functions_map[$folder]}"
    done
fi

# Format the output for the github matrix:
changed_functions_json=$(printf '["%s"]' "$(echo $changed_functions | sed 's/ /","/g')")

# The full list of functions. Uncomment the next block when you want to redeploy all the functions.
# changed_functions_json='["receive-caas-file","create-exception","add-cohort-distribution-data",\
# "create-cohort-distribution","retrieve-cohort-distribution-data",\
# "retrieve-participant-data","allocate-service-provider","transform-data-service","validate-cohort-distribution-record",\
# "demographic-data-management","remove-participant","add-participant","update-participant",\
# "create-participant","demographic-data-service","get-validation-exceptions","mark-participant-as-eligible","\
# "mark-participant-as-ineligible","update-participant-details","file-validation","lookup-validation","static-validation",\
# "remove-validation-exception-data","retrieve-cohort-replay","retrieve-cohort-request-audit","retrieve-mesh-file"]'

# changed_functions_json='["receive-caas-file","create-exception"]'

echo "Final list of functions to rebuild:"
echo "$changed_functions_json"

echo "FUNC_NAMES=$changed_functions_json" >> "$GITHUB_OUTPUT"
