import { APIResponse, expect } from "@playwright/test";

import { config } from "../config/env";

let apiRetry = Number(config.apiRetry);
let waitTime = Number(config.apiWaitTime) || 2000;
let endpoint = "";

export async function validateApiResponse(
  validationJson: any,
  request: any
) {
  if (!Array.isArray(validationJson) || validationJson.length === 0) {
    throw new Error("Invalid validationJson: It must be a non-empty array.");
  }

  for (let attempt = 1; attempt <= apiRetry; attempt++) {
    try {
      let allValid = true; // Flag to track if all validations pass
      let response: APIResponse;
      for (const [index, validationGroup] of validationJson.entries()) {
        console.log(`Processing validation #${index + 1}`);
        const { nhsNumber, screeningServiceId, tableName, columnName, columnValue } = validationGroup.validations;
        if (tableName === "PARTICIPANT_MANAGEMENT") {
          endpoint = config.endpointRetrieveParticipantData;
          response = await request.post(`${endpoint}`, {
            data: {
              NhsNumber: nhsNumber,
              ScreeningService: screeningServiceId
            },
          });

        } else { // Default is always Cohort Distribution Table
          endpoint = config.endpointRetrieveCohortDistributionData;
          response = await request.get(`${endpoint}`, {
            params: {
              rowCount: config.endpointRetrieveCohortDistributionDataRowCount,
            },
          });

        }
       // TODO local cohort temp fix to be applied here if bug exists

        expect(response.status()).toBe(200);

        const responseBody = await response.json();

        //TODO fix logic
        const isValid = responseBody.some((row: any) => {
          const normalizedRow = Object.fromEntries(
            Object.entries(row).map(([key, value]) => [key.toLowerCase(), value])
          );
          return normalizedRow[columnName.toLowerCase()] === columnValue;
        });

        console.info(
          `🚧 Validating ${columnName} == ${columnValue}: ${isValid ? "✅ Record found" : "❌ No matching record found"
          }`
        );

        if (!isValid) {
          allValid = false;
          break; // Exit the validation loop if any validation fails
        }
      }

      if (allValid) {
        console.info("✅ All validations passed. Exiting retry loop.");
        return; // Exit the retry loop if all validations pass
      }
    } catch (error) {
      console.warn(`API request failed for ${endpoint}, attempt ${attempt}`);
    }

    if (attempt < Number(config.apiRetry)) {
      await new Promise((resolve) => setTimeout(resolve, waitTime));
      waitTime += 5000;
    } else {
      throw new Error(
        `API request failed for ${endpoint} after ${apiRetry} attempts`
      );
    }
  }
}
