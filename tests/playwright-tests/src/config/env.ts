import * as dotenv from 'dotenv';
import * as path from 'path';

const env = process.env.NODE_ENV ?? 'dev';
if(process.env.Is_CloudEnvironment){}else{
  dotenv.config({ path: path.resolve(__dirname, `../../../../application/CohortManager/.env`) });
}

const baseURL = process.env.BASE_URL ?? '';
const azureConnectionString = process.env.CAASFOLDER_STORAGE_CONNECTION_STRING ?? '';
const containerName = process.env.CONTAINER_NAME ?? '';
const e2eTestFilesPath = process.env.E2E_TEST_FILES_PATH ?? '';
const apiRetry = Number(process.env.API_RETRIES ?? 1);
const apiWaitTime = Number(process.env.API_WAIT_TIME ?? 2000);
const endpointCohortDistributionDataService = process.env.ENDPOINT_COHORT_DISTRIBUTION_DATA_SERVICE ?? '';
const endpointParticipantManagementDataService = process.env.ENDPOINT_PARTICIPANT_MANAGEMENT_DATA_SERVICE ?? '';
const endpointExceptionManagementDataService = process.env.ENDPOINT_EXCEPTION_MANAGEMENT_DATA_SERVICE ?? '';

export const config = {
  baseURL,
  azureConnectionString,
  containerName,
  endpointCohortDistributionDataService,
  endpointParticipantManagementDataService,
  endpointExceptionManagementDataService,
  cohortDistributionService: 'CohortDistributionDataService',
  participantManagementService: 'ParticipantManagementDataService',
  exceptionManagementService: 'ExceptionManagementDataService',
  e2eTestFilesPath:'e2e/testFiles',
  apiRetry: 8,
  apiWaitTime: 5000,
  nhsNumberKey: 'NHSNumber',
  nhsNumberKeyException: 'NhsNumber',
  uniqueKeyCohortDistribution: 'CohortDistributionId',
  uniqueKeyParticipantManagement: 'ParticipantId',
  uniqueKeyExceptionManagement: 'ExceptionId',
  ignoreValidationKey: 'apiEndpoint'
}
