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
  serviceConfig: ServiceConfig,
  numbers?: string[]
): Promise<void> {
  const { serviceName, idField, endpoint } = serviceConfig;

  try {

    if (numbers && numbers.length > 0) {
      for (const nhsNumber of numbers) {
        try {
          const directDeleteUrl = `${endpoint}api/${serviceName}/byNhsNumber/${nhsNumber}`;
          console.info(`Attempting direct deletion by NHS number: ${nhsNumber}`);
          await request.delete(directDeleteUrl);
        } catch (e) {

        }
      }
    }


  } catch (error) {
    console.error(`Error in ${serviceName} cleanup:`, error);
  }
}



const serviceConfigs = {
  cohortDistribution: {
    serviceName: COHORT_DISTRIBUTION_SERVICE,
    idField: UNIQUE_KEY_COHORT_DISTRIBUTION,
    nhsNumberField: "NHS_NUMBER",
    endpoint: endpointCohortDistributionDataService,

  },
  participantManagement: {
    serviceName: PARTICIPANT_MANAGEMENT_SERVICE,
    idField: UNIQUE_KEY_PARTICIPANT_MANAGEMENT,
    nhsNumberField: "NHS_NUMBER",
    endpoint: endpointParticipantManagementDataService,

  },
  exceptionManagement: {
    serviceName: EXCEPTION_MANAGEMENT_SERVICE,
    idField: "EXCEPTION_ID",
    nhsNumberField: "NHS_NUMBER" ,
    endpoint: endpointExceptionManagementDataService
  },
  participantDemographic: {
    serviceName: PARTICIPANT_DEMOGRAPHIC_SERVICE,
    idField: UNIQUE_KEY_PARTICIPANT_DEMOGRAPHIC,
    nhsNumberField: "NHS_NUMBER",
    endpoint: endpointParticipantDemographicDataService
  }

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
      await cleanDataService(request, config, numbers);
    }
    console.info(`Successfully completed cleaning operations for all services`);
  } catch (error) {
    console.error('Failed to clean one or more services:', error);
    throw error;
  }
}
