import * as dotenv from 'dotenv';
import * as path from 'path';

const env = process.env.NODE_ENV ?? 'dev';
if (process.env.Is_CloudEnvironment) { } else {
  dotenv.config({ path: path.resolve(__dirname, `../../../../application/CohortManager/.env`) });
}

const baseURL = process.env.BASE_URL ?? '';
const azureConnectionString = process.env.CAASFOLDER_STORAGE_CONNECTION_STRING ?? '';
const containerName = process.env.CONTAINER_NAME ?? '';
const endpointCohortDistributionDataService = process.env.ENDPOINT_COHORT_DISTRIBUTION_DATA_SERVICE ?? '';
const endpointParticipantManagementDataService = process.env.ENDPOINT_PARTICIPANT_MANAGEMENT_DATA_SERVICE ?? '';
const endpointParticipantDemographicDataService = process.env.ENDPOINT_PARTICIPANT_DEMOGRAPHIC_DATA_SERVICE ?? '';
const endpointExceptionManagementDataService = process.env.ENDPOINT_EXCEPTION_MANAGEMENT_DATA_SERVICE ?? '';
const endpointBsSelectRetrieveCohortDistributionData = process.env.ENDPOINT_BS_SELECT_RETRIEVE_COHORT_DISTRIBUTION_DATA ?? '';
const endpointBsSelectRetrieveCohortRequestAudit = process.env.ENDPOINT_BS_SELECT_RETRIEVE_COHORT_REQUEST_AUDIT ?? '';
const endpointBsSelectBlockParticipant = process.env.ENDPOINT_BS_SELECT_BLOCK_PARTICIPANT ?? '';

export const config = {
  baseURL,
  azureConnectionString,
  containerName,
  endpointCohortDistributionDataService,
  endpointParticipantManagementDataService,
  endpointExceptionManagementDataService,
  endpointParticipantDemographicDataService,
  endpointBsSelectRetrieveCohortDistributionData: endpointBsSelectRetrieveCohortDistributionData,
  endpointBsSelectRetrieveCohortRequestAudit: endpointBsSelectRetrieveCohortRequestAudit,
  endpointBsSelectBlockParticipant: endpointBsSelectBlockParticipant,
  routeBsSelectRetrieveCohortDistributionData: 'api/RetrieveCohortDistributionData',
  routeBsSelectRetrieveCohortRequestAudit: 'api/RetrieveCohortRequestAudit',
  routeBsSelectBlockParticipant: 'api/BlockParticipant',
  cohortDistributionService: 'CohortDistributionDataService',
  participantManagementService: 'ParticipantManagementDataService',
  exceptionManagementService: 'ExceptionManagementDataService',
  participantDemographicDataService: 'ParticipantDemographicDataService',
  e2eTestFilesPath: 'e2e/testFiles',
  apiTestFilesPath: 'api/testFiles',
  apiRetry: 10,
  apiWaitTime: 5000,
  nhsNumberKey: 'NHSNumber',
  nhsNumberKeyExceptionDemographic: 'NhsNumber',
  uniqueKeyCohortDistribution: 'CohortDistributionId',
  uniqueKeyParticipantManagement: 'ParticipantId',
  uniqueKeyParticipantDemographic: 'ParticipantId',
  uniqueKeyExceptionManagement: 'ExceptionId',
  ignoreValidationKey: 'apiEndpoint'
}
