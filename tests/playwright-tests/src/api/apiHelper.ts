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


export async function cleanupDatabase(numbers: string[], request: any): Promise<boolean> {
  await cleanCohortDistributionDataService(numbers, request);
  await cleanParticipantManagementDataService(numbers, request);
  await cleanExceptionManagementDataService(numbers, request);
  return true
}



export async function validateApiResponse(validationJson: any, request: any): Promise<boolean> {
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
  return status;
}

async function cleanCohortDistributionDataService(numbers: string[], request: any): Promise<void> {
  const keys: number[] = [];
  const responseCohort = await fetchApiResponse(`api/${COHORT_DISTRIBUTION_SERVICE}`, request);
  expect(responseCohort.ok()).toBeTruthy();
  try {
    const responseBodyCohort = await responseCohort.json();
    expect(Array.isArray(responseBodyCohort)).toBeTruthy();

    for (const item of responseBodyCohort) {
      if (numbers.includes(String(item[NHS_NUMBER_KEY]))) {
        keys.push(item.CohortDistributionId);
      }
    }
    console.info(`Keys to delete using ${COHORT_DISTRIBUTION_SERVICE} : ${keys}`);
    for (const key of keys) {
      const response = await request.delete(`${endpointCohortDistributionDataService}api/${COHORT_DISTRIBUTION_SERVICE}/${key}`);
      expect(response.ok()).toBeTruthy();
    }

  } catch (error) {
    console.info(`No response body received  ${COHORT_DISTRIBUTION_SERVICE}:`, error);
  }


}

async function cleanParticipantManagementDataService(numbers: string[], request: any): Promise<void> {
  const keys: number[] = [];
  const responseParticipant = await fetchApiResponse(`api/${PARTICIPANT_MANAGEMENT_SERVICE}`, request);
  expect(responseParticipant.ok()).toBeTruthy();
  try {
    const responseBodyParticipant = await responseParticipant.json();
    expect(Array.isArray(responseBodyParticipant)).toBeTruthy();

    for (const item of responseBodyParticipant) {
      if (numbers.includes(String(item[NHS_NUMBER_KEY]))) {
        keys.push(item.ParticipantId);
      }
    }
    console.info(`Keys to delete using ${PARTICIPANT_MANAGEMENT_SERVICE} : ${keys}`);
    for (const key of keys) {
      const response = await request.delete(`${endpointParticipantManagementDataService}api/${PARTICIPANT_MANAGEMENT_SERVICE}/${key}`);
      expect(response.ok()).toBeTruthy();
    }

  } catch (error) {
    console.error(`No response body received ${PARTICIPANT_MANAGEMENT_SERVICE}:`, error);
  }

}

async function cleanExceptionManagementDataService(numbers: string[], request: any): Promise<void> {
  let keys: number[] = [];
  const responseException = await fetchApiResponse(`api/${EXCEPTION_MANAGEMENT_SERVICE}`, request);
  expect(responseException.ok()).toBeTruthy();
  try {
    const responseBodyException = await responseException.json();
    expect(Array.isArray(responseBodyException)).toBeTruthy();

    for (const item of responseBodyException) {
      if (numbers.includes(String(item[NHS_NUMBER_KEY_EXCEPTION]))) {
        keys.push(item.ExceptionId);
      }
    }
    console.info(`Keys to delete using ${EXCEPTION_MANAGEMENT_SERVICE} : ${keys}`);
    for (const key of keys) {
      const response = await request.delete(`${endpointExceptionManagementDataService}api/${EXCEPTION_MANAGEMENT_SERVICE}/${key}`);
      expect(response.ok()).toBeTruthy();
    }


  } catch (error) {
    console.error(`No response body received ${EXCEPTION_MANAGEMENT_SERVICE}:`, error);

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
