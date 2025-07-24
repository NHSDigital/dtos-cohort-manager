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
const endpointBsSelectDeleteParticipant = process.env.ENDPOINT_BS_SELECT_DELETE_PARTICIPANT ?? '';
const endpointBsSelectUpdateBlockFlag = process.env.ENDPOINT_BS_SELECT_UPDATE_BLOCK_FLAG ?? '';
const endpointBsSelectGetValidationExceptions = process.env.ENDPOINT_BS_SELECT_GET_VALIDATION_EXCEPTIONS ?? '';

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
  endpointBsSelectDeleteParticipant: endpointBsSelectDeleteParticipant,
  endpointBsSelectUpdateBlockFlag: endpointBsSelectUpdateBlockFlag,
  endpointBsSelectGetValidationExceptions: endpointBsSelectGetValidationExceptions,
  routeBsSelectRetrieveCohortDistributionData: 'api/RetrieveCohortDistributionData',
  routeBsSelectRetrieveCohortRequestAudit: 'api/RetrieveCohortRequestAudit',
  routeBsSelectDeleteParticipant: 'api/DeleteParticipant',
  routeBsSelectBlockParticipant: 'api/BlockParticipant',
  routeBsSelectUnblockParticipant: 'api/UnblockParticipant',
  routeGetValidationExceptions: 'api/GetValidationExceptions',
  cohortDistributionService: 'CohortDistributionDataService',
  participantManagementService: 'ParticipantManagementDataService',
  exceptionManagementService: 'ExceptionManagementDataService',
  participantDemographicDataService: 'ParticipantDemographicDataService',
  e2eTestFilesPath: 'e2e/testFiles',
  apiTestFilesPath: 'api/testFiles',
  apiRetry: 8,
  apiWaitTime: 5000,
  nhsNumberKey: 'NHSNumber',
  nhsNumberKeyExceptionDemographic: 'NhsNumber',
  uniqueKeyCohortDistribution: 'CohortDistributionId',
  uniqueKeyParticipantManagement: 'ParticipantId',
  uniqueKeyParticipantDemographic: 'ParticipantId',
  uniqueKeyExceptionManagement: 'ExceptionId',
  ignoreValidationKey: 'apiEndpoint'
}
