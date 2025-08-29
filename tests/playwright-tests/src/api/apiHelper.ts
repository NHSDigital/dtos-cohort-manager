import { APIResponse } from "@playwright/test";

import { config } from "../config/env";
import { assertOnCounts, assertOnRecordDateTimes, assertOnNhsNumber, MatchDynamicType, MatchOnRuleDescriptionDynamic } from "./core/assertOnTypes";



export async function fetchApiResponse(endpoint: string, request: any): Promise<APIResponse> {
  let currentEndPoint = endpoint.toLowerCase()

  switch(endpoint) {
    case `api/${config.cohortDistributionService}`:
      return request.get(`${config.endpointCohortDistributionDataService}${currentEndPoint}`);

    case `api/${config.participantManagementService}`:
      return request.get(`${config.endpointParticipantManagementDataService}${currentEndPoint}`);

    case `api/${config.exceptionManagementService}`:
      return request.get(`${config.endpointExceptionManagementDataService}${currentEndPoint}`);

    case `api/${config.participantDemographicDataService}`:
      return request.get(`${config.endpointParticipantDemographicDataService}${currentEndPoint}`);

    default: 
      throw new Error(`Unknown endpoint: ${endpoint}`);
  }
}

export async function findMatchingObject(endpoint: string, responseBody: any[], apiValidation: any) {
  let nhsNumber: any;
  let matchingObjects: any[] = [];
  let matchingObject: any;

  let nhsNumberKey;
  if (endpoint.includes(config.exceptionManagementService) || endpoint.includes(config.participantDemographicDataService)) {
    nhsNumberKey =  config.nhsNumberKeyExceptionDemographic;
  } else if (endpoint.includes("participantmanagementdataservice") || endpoint.includes("CohortDistributionDataService")) {
    nhsNumberKey = "NHSNumber";
  } else {
    nhsNumberKey = config.nhsNumberKey;
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
    item[nhsNumberKey] === nhsNumber ||
    item.NhsNumber === nhsNumber ||
    item.NHSNumber === nhsNumber
  );

  matchingObject = matchingObjects[matchingObjects.length - 1];

  if (endpoint.includes(config.exceptionManagementService) &&
    (apiValidation.validations.RuleId !== undefined || apiValidation.validations.RuleDescription)) {
    let ruleIdToFind = apiValidation.validations.RuleId;
    let ruleDescToFind = apiValidation.validations.RuleDescription;

    let betterMatches = matchingObjects.filter(record =>
      (ruleIdToFind === undefined || record.RuleId === ruleIdToFind) &&
      (ruleDescToFind === undefined || record.RuleDescription === ruleDescToFind)
    );

    if (betterMatches.length > 0) {
      matchingObject = betterMatches[0];
      console.info(`Found better matching record with NHS Number ${nhsNumber} and RuleId ${ruleIdToFind || 'any'}`);
    }
  }

  return { matchingObject, nhsNumber, matchingObjects };
}


export async function validateFields(apiValidation: any, matchingObject: any, nhsNumber: any, matchingObjects: any): Promise<boolean> {
  const fieldsToValidate = Object.entries(apiValidation.validations).filter(([key]) => key !== config.ignoreValidationKey);

  for (const [fieldName, expectedValue] of fieldsToValidate) {
    switch(fieldName.toLowerCase()){
      case "expectedcount": 
        assertOnCounts(matchingObject, nhsNumber, matchingObjects, fieldName, expectedValue);
        break;
      case "nhsnumber":
        if(!matchingObject) {
          assertOnNhsNumber(expectedValue, nhsNumber);
        }
        break;
      case "recordinsertdatetime":
        assertOnRecordDateTimes(fieldName, expectedValue, nhsNumber, matchingObject);
        break;
      case "recordUpdatedatetime":
        assertOnRecordDateTimes(fieldName, expectedValue, nhsNumber, matchingObject);
        break;
      case "ruledescriptiondynamic":
        MatchOnRuleDescriptionDynamic(matchingObject, nhsNumber);
        break;
      default: 
        MatchDynamicType(matchingObject, nhsNumber, expectedValue, fieldName);
        break;
    }
  }
  return true;
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

