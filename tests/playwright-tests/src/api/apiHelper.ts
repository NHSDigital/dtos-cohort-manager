import { APIResponse, expect } from "@playwright/test";

import { config } from "../config/env";

let apiRetry = Number(config.apiRetry);
let waitTime = Number(config.apiWaitTime) || 2000;
let endpoint = "";

export async function validateApiResponse(validationJson: any, request: any) {
  validateInput(validationJson);

  for (let attempt = 1; attempt <= apiRetry; attempt++) {
    try {
      const allValid = await processValidationGroups(validationJson, request);
      if (allValid) {
        console.info("✅ All validations passed. Exiting retry loop.");
        return;
      }
    } catch (error) {
      console.warn(`API request failed for ${endpoint}, attempt ${attempt}`);
    }

    if (attempt < apiRetry) {
      await delayRetry();
    } else {
      throw new Error(
        `API request failed for ${endpoint} after ${apiRetry} attempts`
      );
    }
  }
}

function validateInput(validationJson: any) {
  if (!Array.isArray(validationJson) || validationJson.length === 0) {
    throw new Error("Invalid validationJson: It must be a non-empty array.");
  }
}

async function processValidationGroups(validationJson: any, request: any) {
  for (const [index, validationGroup] of validationJson.entries()) {
    console.log(`Processing validation #${index + 1}`);
    const { nhsNumber, screeningServiceId, tableName, columnName, columnValue } =
      validationGroup.validations;

    const response = await fetchApiResponse(
      tableName,
      nhsNumber,
      screeningServiceId,
      request
    );

    expect(response.status()).toBe(200);

    const responseBody = await response.json();
    const isValid = validateResponseBody(responseBody, columnName, columnValue);

    if (!isValid) {
      console.info(
        `❌ Validation failed for ${columnName} == ${columnValue}.`
      );
      return false;
    }
  }
  return true;
}

async function fetchApiResponse(
  tableName: string,
  nhsNumber: string,
  screeningServiceId: string,
  request: any
): Promise<APIResponse> {
  if (tableName === "PARTICIPANT_MANAGEMENT") {
    endpoint = config.endpointRetrieveParticipantData;
    return await request.post(`${endpoint}`, {
      data: {
        NhsNumber: nhsNumber,
        ScreeningService: screeningServiceId,
      },
    });
  } else {
    endpoint = config.endpointRetrieveCohortDistributionData;
    return await request.get(`${endpoint}`, {
      params: {
        rowCount: config.endpointRetrieveCohortDistributionDataRowCount,
      },
    });
  }
}

function validateResponseBody(
  responseBody: any,
  columnName: string,
  columnValue: any
): boolean {
  return responseBody.some((row: any) => {
    const normalizedRow = Object.fromEntries(
      Object.entries(row).map(([key, value]) => [key.toLowerCase(), value])
    );
    return normalizedRow[columnName.toLowerCase()] === columnValue;
  });
}

async function delayRetry() {
  await new Promise((resolve) => setTimeout(resolve, waitTime));
  waitTime += 5000;
}
