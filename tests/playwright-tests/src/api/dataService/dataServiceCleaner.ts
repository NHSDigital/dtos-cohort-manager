import { expect } from "@playwright/test";
import { fetchApiResponse } from "../apiHelper";
import { config } from "../../config/env";


const endpointCohortDistributionDataService = config.endpointCohortDistributionDataService;
const endpointParticipantManagementDataService = config.endpointParticipantManagementDataService;
const endpointExceptionManagementDataService = config.endpointExceptionManagementDataService;
const endpointParticipantDemographicDataService = config.endpointParticipantDemographicDataService;


const COHORT_DISTRIBUTION_SERVICE = config.cohortDistributionService;
const PARTICIPANT_MANAGEMENT_SERVICE = config.participantManagementService;
const PARTICIPANT_DEMOGRAPHIC_SERVICE = config.participantDemographicDataService;
const EXCEPTION_MANAGEMENT_SERVICE = config.exceptionManagementService;
const UNIQUE_KEY_COHORT_DISTRIBUTION = config.uniqueKeyCohortDistribution;
const UNIQUE_KEY_PARTICIPANT_MANAGEMENT = config.uniqueKeyParticipantManagement;
const UNIQUE_KEY_EXCEPTION_MANAGEMENT = config.uniqueKeyExceptionManagement;
const UNIQUE_KEY_PARTICIPANT_DEMOGRAPHIC = config.uniqueKeyParticipantDemographic;
const NHS_NUMBER_KEY = config.nhsNumberKey;
const NHS_NUMBER_KEY_EXCEPTION_DEMOGRAPHIC = config.nhsNumberKeyExceptionDemographic;


interface ServiceConfig {
  serviceName: string;
  idField: string;
  endpoint: string;
  matchField?: string;
}

async function cleanDataService(
  request: any,
  serviceConfig: ServiceConfig
): Promise<void> {
  const { serviceName, idField, endpoint } = serviceConfig;

  try {
    const response = await fetchApiResponse(`api/${serviceName}`, request);

    if (!response.ok()) {
      console.warn(`Service ${serviceName} returned status ${response.status()}, skipping cleanup`);
      return;
    }

    if (response.status() === 204) {
      console.info(`No data in the table for ${serviceName}`);
      return;
    }

    // Get response text first to debug JSON parsing issues
    const responseText = await response.text();
    let responseBody;
    try {
      responseBody = JSON.parse(responseText);
    } catch (e) {
      console.error(`Cannot parse JSON for ${serviceName}: ${e}`);
      return;
    }

    if (!Array.isArray(responseBody)) {
      console.warn(`Expected array response from ${serviceName}, got: ${typeof responseBody}`);
      return;
    }

    // Determine which NHS number key to use based on the endpoint
    let nhsNumberKey;
    if (endpoint.includes(EXCEPTION_MANAGEMENT_SERVICE) || endpoint.includes(PARTICIPANT_DEMOGRAPHIC_SERVICE)) {
      nhsNumberKey = NHS_NUMBER_KEY_EXCEPTION_DEMOGRAPHIC;
    } else if (endpoint.includes("participantmanagementdataservice") || endpoint.includes("CohortDistributionDataService")) {
      nhsNumberKey = "NHSNumber";
    } else {
      nhsNumberKey = NHS_NUMBER_KEY;
    }

    // Extract primary keys for deletion
    const keysToDelete = responseBody
      .filter(item => item[idField] !== null && item[idField] !== undefined)
      .map(item => item[idField]);

    // Also extract NHS numbers for logging and verification
    const nhsNumbers = responseBody
      .filter(item => item[nhsNumberKey] !== null && item[nhsNumberKey] !== undefined)
      .map(item => item[nhsNumberKey]);

    console.info(`Keys to delete using ${serviceName}: ${keysToDelete.length} records`);
    console.info(`NHS Numbers in response: ${nhsNumbers.join(', ')}`);

    if (keysToDelete.length === 0) {
      console.info(`No records found in ${serviceName} to delete`);
      return;
    }

    // Delete sequentially to avoid race conditions
    let successCount = 0;
    for (const key of keysToDelete) {
      try {
        const deleteUrl = `${endpoint}api/${serviceName}/${key}`;
        console.info(`Deleting ${serviceName} with ID ${key}`);
        const deleteResponse = await request.delete(deleteUrl);

        if (deleteResponse.ok()) {
          successCount++;
        } else {
          console.warn(`Failed to delete ${serviceName} ID ${key}: ${deleteResponse.status()}`);
        }
      } catch (deleteError) {
        console.error(`Error deleting ${serviceName} ID ${key}:`, deleteError);
      }
    }

    // Verify deletion - check if any NHS numbers still exist
    const verifyResponse = await fetchApiResponse(`api/${serviceName}?nocache=${Date.now()}`, request);
    if (verifyResponse.ok() && verifyResponse.status() !== 204) {
      try {
        const verifyText = await verifyResponse.text();
        if (verifyText && verifyText.trim() !== '') {
          const verifyBody = JSON.parse(verifyText);
          if (Array.isArray(verifyBody) && verifyBody.length > 0) {
            // Check if the same NHS numbers are still present
            const remainingNhsNumbers = verifyBody
              .filter(item => item[nhsNumberKey] !== null && item[nhsNumberKey] !== undefined)
              .map(item => item[nhsNumberKey]);

            if (remainingNhsNumbers.length > 0) {
              console.warn(`❌ Still have ${remainingNhsNumbers.length} NHS numbers in ${serviceName}: ${remainingNhsNumbers.join(', ')}`);
            }
          } else {
            console.info(`✅ Successfully verified deletion in ${serviceName}`);
          }
        }
      } catch (e) {
        console.error(`Error verifying deletion: ${e}`);
      }
    }

    console.info(`Deleted ${successCount}/${keysToDelete.length} records from ${serviceName}`);
  } catch (error) {
    console.error(`Error processing ${serviceName}:`, error);
    // Don't throw to allow other services to continue
  }
}

const serviceConfigs = {
  cohortDistribution: {
    serviceName: COHORT_DISTRIBUTION_SERVICE,
    idField: UNIQUE_KEY_COHORT_DISTRIBUTION,
    endpoint: endpointCohortDistributionDataService
  },
  participantManagement: {
    serviceName: PARTICIPANT_MANAGEMENT_SERVICE,
    idField: UNIQUE_KEY_PARTICIPANT_MANAGEMENT,
    endpoint: endpointParticipantManagementDataService
  },
  exceptionManagement: {
    serviceName: EXCEPTION_MANAGEMENT_SERVICE,
    idField: UNIQUE_KEY_EXCEPTION_MANAGEMENT,
    endpoint: endpointExceptionManagementDataService
  },
  participantDemographic: {
    serviceName: PARTICIPANT_DEMOGRAPHIC_SERVICE,
    idField: UNIQUE_KEY_PARTICIPANT_DEMOGRAPHIC,
    endpoint: endpointParticipantDemographicDataService
  }
  // Add more services as needed
};


export async function cleanDataBaseUsingServices(
  numbers: string[],
  request: any,
  services?: Array<keyof typeof serviceConfigs>
): Promise<void> {
  const servicesToClean = services
    ? services.map(key => serviceConfigs[key])
    : Object.values(serviceConfigs);

  try {

    for (const config of servicesToClean) {
      await cleanDataService(request, config);
    }
    console.info(`Successfully completed cleaning operations for all services`);
  } catch (error) {
    console.error('Failed to clean one or more services:', error);
    throw error;
  }
}
