import { APIResponse, expect } from "@playwright/test";

import { config } from "../config/env";
import { fetchApiResponse, findMatchingObject, validateFields } from "../apiHelper";

const apiRetry = Number(config.apiRetry);
const initialWaitTime = Number(config.apiWaitTime) || 2000;
const endpointCohortDistributionDataService = config.endpointCohortDistributionDataService;
const endpointParticipantManagementDataService = config.endpointParticipantManagementDataService;
const endpointExceptionManagementDataService = config.endpointExceptionManagementDataService;
const endpointParticipantDemographicDataService = config.endpointParticipantDemographicDataService;

const COHORT_DISTRIBUTION_SERVICE = config.cohortDistributionService;
const PARTICIPANT_MANAGEMENT_SERVICE = config.participantManagementService;
const EXCEPTION_MANAGEMENT_SERVICE = config.exceptionManagementService;
const PARTICIPANT_DEMOGRAPHIC_SERVICE = config.participantDemographicDataService;
const NHS_NUMBER_KEY = config.nhsNumberKey;
const NHS_NUMBER_KEY_EXCEPTION_DEMOGRAPHIC = config.nhsNumberKeyExceptionDemographic;
const IGNORE_VALIDATION_KEY = config.ignoreValidationKey;

let waitTime = initialWaitTime;
let response: APIResponse;

export async function validateApiResponse(validationJson: any, request: any): Promise<{ status: boolean; errorTrace?: any }> {


let status = false;
  let endpoint = "";
  let errorTrace: any = undefined;

  for (let attempt = 1; attempt <= apiRetry; attempt++) {
    if (status) break;

    try {
      for (const apiValidation of validationJson) {
        endpoint = apiValidation.validations.apiEndpoint;
        response = await fetchApiResponse(endpoint, request);

        // Handle 204 No Content responses BEFORE parsing JSON
        if (response.status() === 204) {
          // Check if 204 is expected (expectedCount: 0)
          const expectedCount = apiValidation.validations.expectedCount;

          if (expectedCount !== undefined && Number(expectedCount) === 0) {
            console.info(`✅ Status 204: Expected 0 records for endpoint ${endpoint}`);

            // Get NHS number for validation
            const nhsNumber = apiValidation.validations.NHSNumber ||
                            apiValidation.validations.NhsNumber ||
                            apiValidation.validations[NHS_NUMBER_KEY] ||
                            apiValidation.validations[NHS_NUMBER_KEY_EXCEPTION_DEMOGRAPHIC];

            console.info(`Validating fields using 🅰️\t🅿️\tℹ️\t ${endpoint}`);
            console.info(`From Response: null (204 No Content - 0 records as expected)`);
            status = await validateFields(apiValidation, null, nhsNumber, []);
          } else {
            // 204 is unexpected, throw error to trigger retry
            throw new Error(`Status 204: No data found in the table using endpoint ${endpoint}`);
          }
        } else {
          // Normal response handling (200, etc.)
          expect(response.ok()).toBeTruthy();
          const responseBody = await response.json();
          expect(Array.isArray(responseBody)).toBeTruthy();
          const { matchingObject, nhsNumber, matchingObjects } = await findMatchingObject(endpoint, responseBody, apiValidation);
          console.info(`Validating fields using 🅰️\t🅿️\tℹ️\t ${endpoint}`);
          console.info(`From Response ${JSON.stringify(matchingObject, null, 2)}`);
          status = await validateFields(apiValidation, matchingObject, nhsNumber, matchingObjects);
        }
      }
    } catch (error) {
      const errorMsg = `Endpoint: ${endpoint}, Status: ${response?.status?.()}, Error: ${error instanceof Error ? error.stack || error.message : error}`;
      errorTrace = errorMsg;
      if (response?.status?.() === 204) {
        console.info(`ℹ️\t Status 204: No data found in the table using endpoint ${endpoint}`);
      }
    }

    if (attempt < apiRetry && !status) {
      console.info(`🚧 Function processing in progress; will check again using data service ${endpoint} in ${Math.round(waitTime / 1000)} seconds...`);
      await delayRetry();
    }
  }
  waitTime = Number(config.apiWaitTime);
  return { status, errorTrace };
}
function delayRetry() {
    throw new Error("Function not implemented.");
}

