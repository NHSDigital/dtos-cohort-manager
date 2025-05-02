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


    const keysToDelete = responseBody
      .filter(item => item[idField] !== null && item[idField] !== undefined)
      .map(item => item[idField]);

    console.info(`Keys to delete using ${serviceName}: ${keysToDelete.length} records`);

    if (keysToDelete.length === 0) {
      console.info(`No records found in ${serviceName} to delete`);
      return;
    }


    let successCount = 0;
    for (const key of keysToDelete) {
      try {
        const deleteResponse = await request.delete(`${endpoint}api/${serviceName}/${key}`);
        if (deleteResponse.ok()) {
          successCount++;
        } else {
          console.warn(`Failed to delete ${serviceName} ID ${key}: ${deleteResponse.status()}`);
        }
      } catch (deleteError) {
        console.error(`Error deleting ${serviceName} ID ${key}:`, deleteError);
      }
    }


    const verifyResponse = await fetchApiResponse(`api/${serviceName}`, request);
    if (verifyResponse.ok() && verifyResponse.status() !== 204) {
      try {
        const verifyText = await verifyResponse.text();
        if (verifyText && verifyText.trim() !== '') {
          const verifyBody = JSON.parse(verifyText);
          if (Array.isArray(verifyBody) && verifyBody.length > 0) {
            console.warn(`Still have ${verifyBody.length} records in ${serviceName} after deletion`);
          } else {
            console.info(`Successfully verified deletion in ${serviceName}`);
          }
        }
      } catch (e) {
        console.error(`Error verifying deletion: ${e}`);
      }
    }

    console.info(`Successfully deleted ${successCount}/${keysToDelete.length} records from ${serviceName}`);
  } catch (error) {
    console.error(`Error processing ${serviceName}:`, error);

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
