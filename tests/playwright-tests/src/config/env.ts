import * as dotenv from 'dotenv';
import * as path from 'path';

const env = process.env.NODE_ENV ?? 'dev';
if(process.env.Is_CloudEnvironment){}else{
  dotenv.config({ path: path.resolve(__dirname, `../../../../application/CohortManager/.env`) });
}

const baseURL = process.env.BASE_URL ?? '';
const azureConnectionString = process.env.CAASFOLDER_STORAGE_CONNECTION_STRING ?? '';
const containerName = process.env.CONTAINER_NAME ?? '';
const endpointCohortDistributionDataService = process.env.ENDPOINT_COHORT_DISTRIBUTION_DATA_SERVICE ?? '';
const endpointParticipantManagementDataService = process.env.ENDPOINT_PARTICIPANT_MANAGEMENT_DATA_SERVICE ?? '';
const endpointExceptionManagementDataService = process.env.ENDPOINT_EXCEPTION_MANAGEMENT_DATA_SERVICE ?? '';
const endpointExternalBsSelectRetrieveCohortDistributionData = process.env.ENDPOINT_EXTERNAL_BS_SELECT_RETRIEVE_COHORT_DISTRIBUTION_DATA ?? '';
const endpointExternalBsSelectRetrieveCohortRequestAudit = process.env.ENDPOINT_EXTERNAL_BS_SELECT_RETRIEVE_COHORT_REQUEST_AUDIT ?? '';

export const config = {
  baseURL,
  azureConnectionString,
  containerName,
  endpointCohortDistributionDataService,
  endpointParticipantManagementDataService,
  endpointExceptionManagementDataService,
  endpointExternalBsSelectRetrieveCohortDistributionData,
  endpointExternalBsSelectRetrieveCohortRequestAudit,
  cohortDistributionService: 'CohortDistributionDataService',
  participantManagementService: 'ParticipantManagementDataService',
  exceptionManagementService: 'ExceptionManagementDataService',
  e2eTestFilesPath:'e2e/testFiles',
  apiTestFilesPath:'api/testFiles',
  apiRetry: 8,
  apiWaitTime: 5000,
  nhsNumberKey: 'NHSNumber',
  nhsNumberKeyException: 'NhsNumber',
  uniqueKeyCohortDistribution: 'CohortDistributionId',
  uniqueKeyParticipantManagement: 'ParticipantId',
  uniqueKeyExceptionManagement: 'ExceptionId',
  ignoreValidationKey: 'apiEndpoint'
}
