import { APIResponse, expect } from "@playwright/test";

import { config } from "../config/env";

const apiRetry = Number(config.apiRetry);
const initialWaitTime = Number(config.apiWaitTime) || 2000;
const endpointCohortDistributionDataService = config.endpointCohortDistributionDataService;
const endpointParticipantManagementDataService = config.endpointParticipantManagementDataService;
const endpointExceptionManagementDataService = config.endpointExceptionManagementDataService;

const COHORT_DISTRIBUTION_SERVICE = 'CohortDistributionDataService';
const PARTICIPANT_MANAGEMENT_SERVICE = 'ParticipantManagementDataService';
const EXCEPTION_MANAGEMENT_SERVICE = 'ExceptionManagementDataService';
const NHS_NUMBER_KEY = 'NHSNumber';
const NHS_NUMBER_KEY_EXCEPTION = 'NhsNumber';
const IGNORE_VALIDATION_KEY = 'apiEndpoint';

let waitTime = initialWaitTime;
let response: APIResponse;

export async function validateApiResponse(validationJson: any, request: any) {
  let status = false;

  for (let attempt = 1; attempt <= apiRetry; attempt++) {
    if (status) break;

    try {
      for (const apiValidation of validationJson) {
        const endpoint = apiValidation.validations.apiEndpoint;
        response = await fetchApiResponse(endpoint, request);

        expect(response.ok()).toBeTruthy();
        const responseBody = await response.json();
        expect(Array.isArray(responseBody)).toBeTruthy();

        const { matchingObject, nhsNumber } = findMatchingObject(endpoint, responseBody, apiValidation);
        validateFields(apiValidation, matchingObject, nhsNumber);

        status = true;
      }
    } catch (error) {
      if (!status) console.warn(`❌ Validation failed after attempt ${attempt}`);
    }

    if (attempt < apiRetry && !status) {
      console.warn(`🚧 Function processing in progress`);
      console.info(`ℹ️\t Attempt ${attempt} failed. Retrying in ${Math.round(waitTime / 1000)} seconds...`);
      await delayRetry();
    }
  }
}

async function fetchApiResponse(endpoint: string, request: any): Promise<APIResponse> {
  if (endpoint.includes(COHORT_DISTRIBUTION_SERVICE)) {
    return await request.get(`${endpointCohortDistributionDataService}${endpoint.toLowerCase()}`);
  } else if (endpoint.includes(PARTICIPANT_MANAGEMENT_SERVICE)) {
    return await request.get(`${endpointParticipantManagementDataService}${endpoint.toLowerCase()}`);
  } else if (endpoint.includes(EXCEPTION_MANAGEMENT_SERVICE)) {
    return await request.get(`${endpointExceptionManagementDataService}${endpoint.toLowerCase()}`);
  }
  throw new Error(`Unknown endpoint: ${endpoint}`);
}

function findMatchingObject(endpoint: string, responseBody: any[], apiValidation: any) {
  let nhsNumber: any;
  let matchingObjects: any[] = [];
  let matchingObject: any;

  const nhsNumberKey = endpoint.includes(EXCEPTION_MANAGEMENT_SERVICE) ? NHS_NUMBER_KEY_EXCEPTION : NHS_NUMBER_KEY;
  nhsNumber = apiValidation.validations[nhsNumberKey];

  matchingObjects = responseBody.filter((item: Record<string, any>) => item[nhsNumberKey] == nhsNumber);

  matchingObject = endpoint.includes(EXCEPTION_MANAGEMENT_SERVICE)
    ? matchingObjects[0]
    : matchingObjects[matchingObjects.length - 1];

  return { matchingObject, nhsNumber };
}

function validateFields(apiValidation: any, matchingObject: any, nhsNumber: any) {
  const fieldsToValidate = Object.entries(apiValidation.validations).filter(([key]) => key !== IGNORE_VALIDATION_KEY);
  for (const [fieldName, expectedValue] of fieldsToValidate) {
    expect(matchingObject).toHaveProperty(fieldName);
    expect(matchingObject[fieldName]).toBe(expectedValue);
    console.info(`✅ Validation completed for field ${fieldName} with value ${expectedValue} for NHS Number ${nhsNumber}`);
  }
}

async function delayRetry() {
  await new Promise((resolve) => setTimeout(resolve, waitTime));
  waitTime += 5000;
}
