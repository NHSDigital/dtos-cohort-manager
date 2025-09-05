import { APIResponse, expect } from "@playwright/test";
import { config } from "../../config/env";
import { ApiResponse } from "../core/types";



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
    let endpoint = "";
    let errorTrace: any = undefined;
    let status = false;

    let resultFromPolling: {  apiResponse: APIResponse, status: boolean, errorTrace: any} | null = null;

    for (const apiValidation of validationJson) {
        endpoint = apiValidation.validations.apiEndpoint;
        resultFromPolling = await pollAPI(endpoint, apiValidation, request);    
    }

    if(resultFromPolling) {
        status = resultFromPolling.status
        errorTrace = resultFromPolling.errorTrace
    }
    return {status, errorTrace };
}


async function pollAPI(endpoint: string, apiValidation: any, request: any): Promise<{  apiResponse: APIResponse, status: boolean, errorTrace: any}> {
    let apiResponse: APIResponse | null = null;
    let i = 0;
    let errorTrace: any = undefined;

    let maxNumberOfRetries = config.maxNumberOfRetries;
    let maxTimeBetweenRequests = config.maxTimeBetweenRequests;
    let status = false;
    console.info(`now trying request for ${maxNumberOfRetries} retries`);
       while (i < Number(maxNumberOfRetries)) {
        try{
                apiResponse =  await fetchApiResponse(endpoint, request);
                switch(apiResponse.status()) {
                    case 204:
                        console.info("now handling no content response");
                        const expectedCount = apiValidation.validations.expectedCount;
                        status = await HandleNoContentResponse(expectedCount, apiValidation, endpoint);
                        break;
                    case 200: 
                        console.info("now handling OK response");
                        status = await handleOKResponse(apiValidation, endpoint, apiResponse);
                        break;
                    default: 
                        console.error("there was an error when handling response from ");
                        break;
                }
                console.log("api status code is: ", apiResponse.status());
                if(status) {
                    break;
                }
                i++;

                console.info(`http response completed ${i}/${maxNumberOfRetries} of number of retries`);
                await new Promise(res => setTimeout(res, maxTimeBetweenRequests));            
        
            } catch (error) {
                const errorMsg = `Endpoint: ${endpoint}, Status: ${apiResponse?.status?.()}, Error: ${error instanceof Error ? error.stack || error.message : error}`;
                errorTrace = errorMsg;
                if (apiResponse?.status?.() === 204) {
                    console.info(`ℹ️\t Status 204: No data found in the table using endpoint ${endpoint}`);
                }
            }
       }
       

        if (!apiResponse) {
            throw new Error("apiResponse was never assigned");
        }

        return {apiResponse, status, errorTrace};
}

async function HandleNoContentResponse(expectedCount: number, apiValidation: any, endpoint: string): Promise<boolean> {
    if (expectedCount !== undefined && Number(expectedCount) === 0) {
        console.info(`✅ Status 204: Expected 0 records for endpoint ${endpoint}`);

        // Get NHS number for validation
        const nhsNumber = apiValidation.validations.NHSNumber ||
                        apiValidation.validations.NhsNumber ||
                        apiValidation.validations[NHS_NUMBER_KEY] ||
                        apiValidation.validations[NHS_NUMBER_KEY_EXCEPTION_DEMOGRAPHIC];

        console.info(`Validating fields using 🅰️\t🅿️\tℹ️\t ${endpoint}`);
        console.info(`From Response: null (204 No Content - 0 records as expected)`);
        let status = await validateFields(apiValidation, null, nhsNumber, []);
        return status;
    } else {
        // 204 is unexpected, log error and return false to trigger retry
        console.warn(`Status 204: No data found in the table using endpoint ${endpoint}`);
        return false;
    }
}

async function handleOKResponse(apiValidation: any, endpoint: string, response: any ) : Promise<boolean>{
   // Normal response handling (200, etc.)
    expect(response.ok()).toBeTruthy();
    const responseBody = await response.json();
    expect(Array.isArray(responseBody)).toBeTruthy();
    const { matchingObject, nhsNumber, matchingObjects } = await findMatchingObject(endpoint, responseBody, apiValidation);
    console.info(`Validating fields using 🅰️\t🅿️\tℹ️\t ${endpoint}`);
    console.info(`From Response ${JSON.stringify(matchingObject, null, 2)}`);
    let status = await validateFields(apiValidation, matchingObject, nhsNumber, matchingObjects);

    return status;
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

export async function pollApiForOKResponse(httpRequest: () => Promise<ApiResponse>): Promise<ApiResponse>{
    let apiResponse: ApiResponse | null = null;
    let i = 0;
    let maxNumberOfRetries = config.maxNumberOfRetries;
    let maxTimeBetweenRequests = config.maxTimeBetweenRequests;

    console.info(`now trying request for ${maxNumberOfRetries} retries`);
    while (i < Number(maxNumberOfRetries)) {
        try {
            apiResponse =  await httpRequest();
            if (apiResponse.status == 200) {
                console.info("200 response found") 
                break;
            }
        }
        catch(exception) {
            console.error("Error reading request body:", exception);
        }
        i++;

        console.info(`http response completed ${i}/${maxNumberOfRetries} of number of retries`);
        await new Promise(res => setTimeout(res, maxTimeBetweenRequests));
    }

    if (!apiResponse) {
        throw new Error("apiResponse was never assigned");
    }
    return apiResponse;
};
