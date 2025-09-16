import { expect } from "@playwright/test";
import { fetchApiResponse } from "../apiHelper";
import { config } from "../../config/env";


const endpointCohortDistributionDataService = config.endpointCohortDistributionDataService;
const endpointParticipantManagementDataService = config.endpointParticipantManagementDataService;
const endpointExceptionManagementDataService = config.endpointExceptionManagementDataService;
const endpointParticipantDemographicDataService = config.endpointParticipantDemographicDataService;
const endpointNemsSubscriptionDataDataService = config.endpointNemsSubscriptionDataDataService;


const COHORT_DISTRIBUTION_SERVICE = config.cohortDistributionService;
const PARTICIPANT_MANAGEMENT_SERVICE = config.participantManagementService;
const PARTICIPANT_DEMOGRAPHIC_SERVICE = config.participantDemographicDataService;
const EXCEPTION_MANAGEMENT_SERVICE = config.exceptionManagementService;
const NHS_NUMBER_KEY = config.nhsNumberKey;
const NHS_NUMBER_KEY_EXCEPTION_DEMOGRAPHIC = config.nhsNumberKeyExceptionDemographic;
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
    expect(response.ok()).toBeTruthy();

    if (response.status() === 204) {
      console.info(`No data in the table for ${serviceName}`);
      return;
    }

    const responseBody = await response.json();
    expect(Array.isArray(responseBody)).toBeTruthy();

    // Extract ALL IDs from the response without filtering
    const keysToDelete = responseBody.map((item: { [x: string]: any; }) => item[idField]);

    console.info(`Keys to delete using ${serviceName}: ${keysToDelete.length} records`);

    if (keysToDelete.length === 0) {
      console.info(`No records found in ${serviceName} to delete`);
      return;
    }

    await Promise.all(
      keysToDelete.map(async (key: number) => {
        const deleteResponse = await request.delete(`${endpoint}api/${serviceName}/${key}`);
        expect(deleteResponse.ok()).toBeTruthy();
        return deleteResponse;
      })
    );

    console.info(`Successfully deleted ${keysToDelete.length} records from ${serviceName}`);
  } catch (error) {
    console.error(`Error processing ${serviceName}:`, error);
    throw error;
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
  },
  nemsSubscription: {
    serviceName: 'NemsSubscriptionDataService',
    idField: 'SubscriptionId',
    endpoint: endpointNemsSubscriptionDataDataService
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
    await Promise.all(
      servicesToClean.map(config =>
        cleanDataService(request, config)
      )
    );
    console.info(`Successfully completed cleaning operations for all services`);
  } catch (error) {
    console.error('Failed to clean one or more services:', error);
    throw error;
  }
}
