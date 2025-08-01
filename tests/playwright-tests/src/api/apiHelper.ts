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

export async function fetchApiResponse(endpoint: string, request: any): Promise<APIResponse> {

  if (endpoint.includes(COHORT_DISTRIBUTION_SERVICE)) {
    return await request.get(`${endpointCohortDistributionDataService}${endpoint.toLowerCase()}`);
  } else if (endpoint.includes(PARTICIPANT_MANAGEMENT_SERVICE)) {
    return await request.get(`${endpointParticipantManagementDataService}${endpoint.toLowerCase()}`);
  } else if (endpoint.includes(EXCEPTION_MANAGEMENT_SERVICE)) {
    return await request.get(`${endpointExceptionManagementDataService}${endpoint.toLowerCase()}`);
  } else if (endpoint.includes(PARTICIPANT_DEMOGRAPHIC_SERVICE)) {
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

      let actualCount = 0;
      if (matchingObjects && Array.isArray(matchingObjects)) {
        actualCount = matchingObjects.length;
      } else if (matchingObjects === null || matchingObjects === undefined) {
        actualCount = 0;
        console.warn(`⚠️ matchingObjects is ${matchingObjects === null ? 'null' : 'undefined'} for NHS Number ${nhsNumber}`);
      } else {
        actualCount = 1;
        console.warn(`⚠️ matchingObjects is not an array for NHS Number ${nhsNumber}, treating as single object`);
      }

      console.info(`📊 Actual count: ${actualCount}, Expected count: ${expectedValue} for NHS Number ${nhsNumber}`);

      const expectedCount = Number(expectedValue);

      if (isNaN(expectedCount)) {
        throw new Error(`❌ expectedCount value '${expectedValue}' is not a valid number for NHS Number ${nhsNumber}`);
      }

      // Perform the assertion
      try {
        expect(actualCount).toBe(expectedCount);
        console.info(`✅ Count check completed for field ${fieldName} with value ${expectedValue} for NHS Number ${nhsNumber}`);
      } catch (error) {
        console.error(`❌ Count check failed for NHS Number ${nhsNumber}: Expected ${expectedCount}, but got ${actualCount}`);
        throw error;
      }
    }

    // Handle NHS Number validation specially for 204 responses
    else if ((fieldName === "NHSNumber" || fieldName === "NhsNumber") && !matchingObject) {
      console.info(`🚧 Validating NHS Number field ${fieldName} for 204 response`);

      // For 204 responses, validate that we searched for the correct NHS number
      const expectedNhsNumber = Number(expectedValue);
      const actualNhsNumber = Number(nhsNumber);

      try {
        expect(actualNhsNumber).toBe(expectedNhsNumber);
        console.info(`✅ NHS Number validation completed: searched for ${actualNhsNumber}, expected ${expectedNhsNumber}`);
      } catch (error) {
        console.error(`❌ NHS Number validation failed: searched for ${actualNhsNumber}, expected ${expectedNhsNumber}`);
        throw error;
      }
    }

    else if (fieldName === 'RecordInsertDateTime' || fieldName === 'RecordUpdateDateTime') {
      console.info(`🚧 Validating timestamp field ${fieldName} for NHS Number ${nhsNumber}`);

      if (!matchingObject && expectedValue !== null && expectedValue !== undefined) {
        throw new Error(`❌ No matching object found for NHS Number ${nhsNumber} but expected to validate field ${fieldName}`);
      }

      if (!matchingObject && (expectedValue === null || expectedValue === undefined)) {
        console.info(`ℹ️ Skipping validation for ${fieldName} as no matching object found and no expected value for NHS Number ${nhsNumber}`);
        continue;
      }

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

    // ✅ Custom dynamic rule description handling
    else if (fieldName === 'RuleDescriptionDynamic') {
      const actualValue = matchingObject['RuleDescription'];
      console.info(`Actual RuleDescription: "${actualValue}"`);

      // Regex based on message requirement
      //const dynamicPattern = /Unable to add to cohort distribution\. As participant with ParticipantId: \d+\.\sHas an Exception against it/;
      //const dynamicPattern = /Unable to add to cohort distribution\. As participant \d+\.\shas triggered a validation exception/;
      //const dynamicPattern = /^Unable to add to cohort distribution\. As participant (\d+) has triggered a validation exception$/;
      const dynamicPattern = /Unable to add to cohort distribution\. As participant \d+ has triggered a validation exception/;

      try {
        expect(actualValue).toMatch(dynamicPattern);
        console.info(`✅ Dynamic message validation passed for NHS Number ${nhsNumber}`);
      } catch (error) {
        console.info(`❌ Dynamic message validation failed!`);
        throw error;
      }
    }

    else {
      console.info(`🚧 Validating field ${fieldName} with expected value ${expectedValue} for NHS Number ${nhsNumber}`);

      if (!matchingObject && expectedValue !== null && expectedValue !== undefined) {
        throw new Error(`❌ No matching object found for NHS Number ${nhsNumber} but expected to validate field ${fieldName}`);
      }

      if (!matchingObject && (expectedValue === null || expectedValue === undefined)) {
        console.info(`ℹ️ Skipping validation for ${fieldName} as no matching object found and no expected value for NHS Number ${nhsNumber}`);
        continue;
      }

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
