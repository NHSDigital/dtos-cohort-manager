import { APIResponse, expect } from "@playwright/test";

import { config } from "../config/env";

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

        const { matchingObject, nhsNumber } = await findMatchingObject(endpoint, responseBody, apiValidation, false);
        status = await validateFields(apiValidation, matchingObject, nhsNumber);

      }
    } catch (error) {
    }

    if (attempt < apiRetry && !status) {
      console.info(`🚧 Function processing in progress; will check again in ${Math.round(waitTime / 1000)} seconds...`);
      await delayRetry();
    }
  }
  waitTime = Number(config.apiWaitTime);
  return status;
}

export async function fetchApiResponse(endpoint: string, request: any): Promise<APIResponse> {
  if (endpoint.includes(COHORT_DISTRIBUTION_SERVICE)) {
    return await request.get(`${endpointCohortDistributionDataService}${endpoint.toLowerCase()}`);
  } else if (endpoint.includes(PARTICIPANT_MANAGEMENT_SERVICE)) {
    return await request.get(`${endpointParticipantManagementDataService}${endpoint.toLowerCase()}`);
  } else if (endpoint.includes(EXCEPTION_MANAGEMENT_SERVICE)) {
    return await request.get(`${endpointExceptionManagementDataService}${endpoint.toLowerCase()}`);
  } else if(endpoint.includes(PARTICIPANT_DEMOGRAPHIC_SERVICE)) {
    return await request.get(`${endpointParticipantDemographicDataService}${endpoint.toLowerCase()}`);
  }
  throw new Error(`Unknown endpoint: ${endpoint}`);
}

async function findMatchingObject(endpoint: string, responseBody: any[], apiValidation: any, includeCount: boolean = true ) {
  let nhsNumber: any;
  let matchingObjects: any[] = [];
  let matchingObject: any;


  const nhsNumberKey = endpoint.includes(EXCEPTION_MANAGEMENT_SERVICE)
    ? NHS_NUMBER_KEY_EXCEPTION_DEMOGRAPHIC
    : endpoint.includes(PARTICIPANT_DEMOGRAPHIC_SERVICE)
    ? NHS_NUMBER_KEY_EXCEPTION_DEMOGRAPHIC
    : endpoint.includes("participantmanagementdataservice")
    ? "NHSNumber"
    : endpoint.includes("CohortDistributionDataService")
    ? "NHSNumber"
    : NHS_NUMBER_KEY;


  nhsNumber = apiValidation.validations[nhsNumberKey];


  if (!nhsNumber) {
    if (apiValidation.validations.NhsNumber) {
      nhsNumber = apiValidation.validations.NhsNumber;
    } else if (apiValidation.validations.NHSNumber) {
      nhsNumber = apiValidation.validations.NHSNumber;
    }
  }


  matchingObjects = responseBody.filter((item: Record<string, any>) =>
    item[nhsNumberKey] == nhsNumber ||
    item.NhsNumber == nhsNumber ||
    item.NHSNumber == nhsNumber
  );


  matchingObject = matchingObjects[matchingObjects.length - 1];


  if (endpoint.includes(EXCEPTION_MANAGEMENT_SERVICE) &&
      (apiValidation.validations.RuleId !== undefined || apiValidation.validations.RuleDescription)) {

    const ruleIdToFind = apiValidation.validations.RuleId;
    const ruleDescToFind = apiValidation.validations.RuleDescription;


    const betterMatches = matchingObjects.filter(record =>
      (ruleIdToFind === undefined || record.RuleId === ruleIdToFind) &&
      (ruleDescToFind === undefined || record.RuleDescription === ruleDescToFind)
    );

    if (betterMatches.length > 0) {

      matchingObject = betterMatches[0];
      console.log(`Found better matching record with NHS Number ${nhsNumber} and RuleId ${ruleIdToFind || 'any'}`);
    }
  }

  if (includeCount) {
    return { matchingObject, nhsNumber, count: matchingObjects.length };
  } else {
    return { matchingObject, nhsNumber };
  }
}


async function validateFields(apiValidation: any, matchingObject: any, nhsNumber: any): Promise<boolean> {
  const fieldsToValidate = Object.entries(apiValidation.validations).filter(([key]) => key !== IGNORE_VALIDATION_KEY);
  try{
    for (const [fieldName, expectedValue] of fieldsToValidate) {
      console.info(`🚧 Validating field ${fieldName} with expected value ${expectedValue} for NHS Number ${nhsNumber}`);
      expect(matchingObject).toHaveProperty(fieldName);
      expect(matchingObject[fieldName]).toBe(expectedValue);
      console.info(`✅ Validation completed for field ${fieldName} with value ${expectedValue} for NHS Number ${nhsNumber}`);
    }
    return true;
  } catch (error) {
    return false;
  }


}

export async function validateApiResponseCount(validationJson: any, request: any, expectedCount: number): Promise<boolean> {
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

        // Get the matching object, NHS number, and count of matching records
        const { matchingObject, nhsNumber, count } = await findMatchingObject(endpoint, responseBody, apiValidation, true);

        // Log the count of matching records
        console.info(`🚧 Found ${count} matching records in ${endpoint} for NHS Number ${nhsNumber}`);

        status = await validateCount(nhsNumber, count ?? 0, expectedCount);

      }
    } catch (error) {
    }

    if (attempt < apiRetry && !status) {
      console.info(`🚧 Function processing in progress; will check again in ${Math.round(waitTime / 1000)} seconds...`);
      await delayRetry();
    }
  }
  waitTime = Number(config.apiWaitTime); //Reset to original value on exit.
  return status;
}



async function validateCount(nhsNumber: any, count: number, expectedCount: number): Promise<boolean> {
  if (count !== expectedCount) {
    console.error(`❌ Validation failed: Expected ${expectedCount} matching record(s), but found ${count} for NHS Number ${nhsNumber}`);
    return false; // Fail the validation immediately
  }

  console.info(`✅ Validation completed: ${expectedCount} record(s) validated for NHS Number ${nhsNumber}`);
  return true;
}

async function delayRetry() {
  await new Promise((resolve) => setTimeout(resolve, waitTime));
  waitTime += 5000;
}


export async function checkMappingsByIndex(
  original: Array<{ requestId: string; nhsNumber: string }>,
  shifted: Array<{ requestId: string; nhsNumber: string }>
): Promise<boolean> {
  const uniqueOriginalRequestIds: string[] = [];
  original.forEach(item => {
    if (!uniqueOriginalRequestIds.includes(item.requestId)) {
      uniqueOriginalRequestIds.push(item.requestId);
    }
  });
  const uniqueShiftedRequestIds: string[] = [];
  shifted.forEach(item => {
    if (!uniqueShiftedRequestIds.includes(item.requestId)) {
      uniqueShiftedRequestIds.push(item.requestId);
    }
  });

  let allMatched = true;
  for (let i = 0; i < uniqueShiftedRequestIds.length; i++) {
    const shiftedRequestId = uniqueShiftedRequestIds[i];
    const originalNextRequestId = uniqueOriginalRequestIds[i + 1];

    if (!originalNextRequestId) {
      console.info(`No next request ID for index ${i}`);
      continue;
    }
    const shiftedNhsNumbers = shifted
      .filter(item => item.requestId === shiftedRequestId)
      .map(item => item.nhsNumber)
      .sort((a, b) => a.localeCompare(b));

    const originalNextNhsNumbers = original
      .filter(item => item.requestId === originalNextRequestId)
      .map(item => item.nhsNumber)
      .sort((a, b) => a.localeCompare(b));

    if (shiftedNhsNumbers.length !== originalNextNhsNumbers.length) {
      console.info(`Length mismatch for index ${i}`);
      console.info(`Shifted [${shiftedRequestId}]: ${shiftedNhsNumbers.length} items`);
      console.info(`Original Next [${originalNextRequestId}]: ${originalNextNhsNumbers.length} items`);
      allMatched = false;
      continue;
    }

    const allNhsNumbersMatch = shiftedNhsNumbers.every(
      (nhsNumber, index) => nhsNumber === originalNextNhsNumbers[index]
    );

    if (!allNhsNumbersMatch) {
      console.error(`❌ NHS numbers don't match for index ${i}`);
      console.warn(`Shifted [${shiftedRequestId}]: ${shiftedNhsNumbers}`);
      console.warn(`Original Next [${originalNextRequestId}]: ${originalNextNhsNumbers}`);
      allMatched = false;
    } else {
      console.info(`✅ NHS numbers match for index ${i} (${shiftedRequestId} -> ${originalNextRequestId})`);
    }
  }

  return allMatched;
}
