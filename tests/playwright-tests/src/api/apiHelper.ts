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

        expect(response.ok()).toBeTruthy();
        const responseBody = await response.json();
        expect(Array.isArray(responseBody)).toBeTruthy();
        const { matchingObject, nhsNumber, matchingObjects } = await findMatchingObject(endpoint, responseBody, apiValidation);
        console.info(`Validating fields using 🅰️\t🅿️\tℹ️\t ${endpoint}`);
        console.info(`From Response ${JSON.stringify(matchingObject, null, 2)}`);
        status = await validateFields(apiValidation, matchingObject, nhsNumber, matchingObjects);
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

export async function fetchApiResponse(endpoint: string, request: any): Promise<APIResponse> {

  if (endpoint.includes(COHORT_DISTRIBUTION_SERVICE)) {
    console.error("URL: " + `${endpointCohortDistributionDataService}${endpoint.toLowerCase()}`);
    return await request.get(`${endpointCohortDistributionDataService}${endpoint.toLowerCase()}`);
  } else if (endpoint.includes(PARTICIPANT_MANAGEMENT_SERVICE)) {
    console.error("URL: " + `${endpointParticipantManagementDataService}${endpoint.toLowerCase()}`);
    return await request.get(`${endpointParticipantManagementDataService}${endpoint.toLowerCase()}`);
  } else if (endpoint.includes(EXCEPTION_MANAGEMENT_SERVICE)) {
    console.error("URL: " + `${endpointExceptionManagementDataService}${endpoint.toLowerCase()}`);
    return await request.get(`${endpointExceptionManagementDataService}${endpoint.toLowerCase()}`);
  } else if (endpoint.includes(PARTICIPANT_DEMOGRAPHIC_SERVICE)) {
    console.error("URL: " + `${endpointParticipantDemographicDataService}${endpoint.toLowerCase()}`);
    return await request.get(`${endpointParticipantDemographicDataService}${endpoint.toLowerCase()}`);
  }
  throw new Error(`Unknown endpoint: ${endpoint}`);
}

async function findMatchingObject(endpoint: string, responseBody: any[], apiValidation: any) {
  let nhsNumber: any;
  let matchingObjects: any[] = [];
  let matchingObject: any;


  let nhsNumberKey;
  if (endpoint.includes(EXCEPTION_MANAGEMENT_SERVICE) || endpoint.includes(PARTICIPANT_DEMOGRAPHIC_SERVICE)) {
    nhsNumberKey = NHS_NUMBER_KEY_EXCEPTION_DEMOGRAPHIC;
  } else if (endpoint.includes("participantmanagementdataservice") || endpoint.includes("CohortDistributionDataService")) {
    nhsNumberKey = "NHSNumber";
  } else {
    nhsNumberKey = NHS_NUMBER_KEY;
  }

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

  return { matchingObject, nhsNumber, matchingObjects };
}


async function validateFields(apiValidation: any, matchingObject: any, nhsNumber: any, matchingObjects: any): Promise<boolean> {
  const fieldsToValidate = Object.entries(apiValidation.validations).filter(([key]) => key !== IGNORE_VALIDATION_KEY);

  for (const [fieldName, expectedValue] of fieldsToValidate) {
    if (fieldName === "expectedCount") {
      console.info(`🚧 Count check with expected value ${expectedValue} for NHS Number ${nhsNumber}`);
      const actualCount = matchingObjects.length;
      expect(actualCount).toBe(expectedValue);
      console.info(`✅ Count check completed for field ${fieldName} with value ${expectedValue} for NHS Number ${nhsNumber}`);
    }
    // Special handling for timestamp fields
    else if (fieldName === 'RecordInsertDateTime' || fieldName === 'RecordUpdateDateTime') {
      console.info(`🚧 Validating timestamp field ${fieldName} for NHS Number ${nhsNumber}`);

      expect(matchingObject).toHaveProperty(fieldName);
      const actualValue = matchingObject[fieldName];


      if (typeof expectedValue === 'string' && expectedValue.startsWith('PATTERN:')) {
        const pattern = expectedValue.substring('PATTERN:'.length);
        console.info(`Validating timestamp against pattern: ${pattern}`);


        const formatMatch = validateTimestampFormat(actualValue, pattern);

        if (formatMatch) {
          console.info(`✅ Timestamp matches pattern for ${fieldName}`);
        } else {
          console.error(`❌ Timestamp doesn't match pattern for ${fieldName}`);
          expect(formatMatch).toBe(true);
        }
      } else {

        if (expectedValue === actualValue) {
          console.info(`✅ Timestamp exact match for ${fieldName}`);
        } else {
          try {
            const expectedDate = new Date(expectedValue as string);
            const actualDate = new Date(actualValue);


            const expectedTimeWithoutMs = new Date(expectedDate);
            expectedTimeWithoutMs.setMilliseconds(0);
            const actualTimeWithoutMs = new Date(actualDate);
            actualTimeWithoutMs.setMilliseconds(0);


            if (expectedTimeWithoutMs.getTime() === actualTimeWithoutMs.getTime()) {
              console.info(`✅ Timestamp matches (ignoring milliseconds) for ${fieldName}`);
            } else {

              const timeDiff = Math.abs(expectedDate.getTime() - actualDate.getTime());
              const oneMinute = 60 * 1000;


              if (timeDiff <= oneMinute) {
                console.info(`✅ Timestamp within acceptable range (±1 minute) for ${fieldName}`);
              } else {

                expect(actualValue).toBe(expectedValue);
              }
            }
          } catch (e) {
            console.error(`Error validating timestamp: ${e}`);
            expect(actualValue).toBe(expectedValue);
          }
        }
      }

      console.info(`✅ Validation completed for timestamp field ${fieldName} for NHS Number ${nhsNumber}`);
    }
    else {
      console.info(`🚧 Validating field ${fieldName} with expected value ${expectedValue} for NHS Number ${nhsNumber}`);

      expect(matchingObject).toHaveProperty(fieldName);
      expect(matchingObject[fieldName]).toBe(expectedValue);
      console.info(`✅ Validation completed for field ${fieldName} with value ${expectedValue} for NHS Number ${nhsNumber}`);
    }
  }
  return true;
}

// Helper function to validate timestamp format
function validateTimestampFormat(timestamp: string, pattern: string): boolean {
  if (!timestamp) return false;

  console.info(`Actual timestamp: ${timestamp}`);


  if (pattern === 'yyyy-MM-ddTHH:mm:ss' || pattern === 'yyyy-MM-ddTHH:mm:ss.SSS') {

    return /^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(\.\d+)?$/.test(timestamp);
  }
  else if (pattern === 'yyyy-MM-dd') {

    return /^\d{4}-\d{2}-\d{2}$/.test(timestamp);
  }
  else {

    return !isNaN(new Date(timestamp).getTime());
  }
}

async function delayRetry() {
  await new Promise((resolve) => setTimeout(resolve, waitTime));
  waitTime += 10000;
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
