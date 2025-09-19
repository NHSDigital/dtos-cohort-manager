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
const endpointServiceNowCasesDataService = process.env.ENDPOINT_SERVICENOW_CASES_DATA_SERVICE ?? '';
const endpointExceptionManagementDataService = process.env.ENDPOINT_EXCEPTION_MANAGEMENT_DATA_SERVICE ?? '';
const endpointNemsSubscriptionDataDataService = process.env.ENDPOINT_SUBSCRIPTION_NEMS_DATA_SERVICE ?? '';
const endpointBsSelectRetrieveCohortDistributionData = process.env.ENDPOINT_BS_SELECT_RETRIEVE_COHORT_DISTRIBUTION_DATA ?? '';
const endpointBsSelectRetrieveCohortRequestAudit = process.env.ENDPOINT_BS_SELECT_RETRIEVE_COHORT_REQUEST_AUDIT ?? '';
const endpointBsSelectDeleteParticipant = process.env.ENDPOINT_BS_SELECT_DELETE_PARTICIPANT ?? '';
const endpointBsSelectUpdateBlockFlag = process.env.ENDPOINT_BS_SELECT_UPDATE_BLOCK_FLAG ?? '';
const endpointBsSelectGetValidationExceptions = process.env.ENDPOINT_BS_SELECT_GET_VALIDATION_EXCEPTIONS ?? '';
const endpointSerNowReceiveParticipant = process.env.ENDPOINT_SERVICE_NOW_MESSAGE_HANDLER ?? '';
const endpointNemsGetSubscriberId = process.env.ENDPOINT_SUB_TO_NEMS ?? '';
const invalidEndpointSerNow = process.env.INVALID_ENDPOINT_SERVICE_NOW_MESSAGE_HANDLER ?? '';
const participantPayloadPath = process.env.PARTICIPANT_PAYLOAD_PATH ?? '';
const createPDSDemographicEnv = process.env.ENDPOINT_PDS_DEMOGRAPHIC ?? '';
const subToNemsEndPoint = process.env.ENDPOINT_SUB_TO_NEMS ?? '';
// Optional: dedicated Manage-CAAS Subscribe base for Epic 4f
const manageCaasSubscribeEndPoint = process.env.ENDPOINT_MANAGE_CAAS_SUBSCRIBE ?? '';
const wireMockUrl = process.env.WIREMOCK_URL ?? '';

export const config = {
  baseURL,
  azureConnectionString,
  containerName,
  endpointCohortDistributionDataService,
  endpointParticipantManagementDataService,
  endpointExceptionManagementDataService,
  endpointParticipantDemographicDataService,
  endpointServiceNowCasesDataService,
  endpointNemsSubscriptionDataDataService,
  endpointBsSelectRetrieveCohortDistributionData: endpointBsSelectRetrieveCohortDistributionData,
  endpointBsSelectRetrieveCohortRequestAudit: endpointBsSelectRetrieveCohortRequestAudit,
  endpointBsSelectDeleteParticipant: endpointBsSelectDeleteParticipant,
  endpointBsSelectUpdateBlockFlag: endpointBsSelectUpdateBlockFlag,
  endpointBsSelectGetValidationExceptions: endpointBsSelectGetValidationExceptions,
  endpointSerNowReceiveParticipant: endpointSerNowReceiveParticipant,
  endpointNemsGetSubscriberId: endpointNemsGetSubscriberId,
  createPDSDemographic: createPDSDemographicEnv,
  invalidEndpointSerNow: invalidEndpointSerNow,
  SubToNems: subToNemsEndPoint,
  // Prefer dedicated Manage-CAAS Subscribe endpoint if provided, fallback to SubToNems
  ManageCaasSubscribe: manageCaasSubscribeEndPoint || subToNemsEndPoint,
  wireMockUrl: wireMockUrl,
  SubToNemsPath: 'api/Subscribe',
  CheckNemsSubPath:'api/CheckSubscriptionStatus',
  routeBsSelectRetrieveCohortDistributionData: 'api/RetrieveCohortDistributionData',
  routeBsSelectRetrieveCohortRequestAudit: 'api/RetrieveCohortRequestAudit',
  routeBsSelectDeleteParticipant: 'api/DeleteParticipant',
  routeBsSelectBlockParticipant: 'api/BlockParticipant',
  routeBsSelectUnblockParticipant: 'api/UnblockParticipant',
  routeGetValidationExceptions: 'api/GetValidationExceptions',
  routeSerNowReceiveParticipant: 'api/servicenow/receive',
  nemsSubscriberDataService: 'CheckSubscriptionStatus',
  invalidRouteSerNowEndpoint: 'api/serviceno/receive',
  cohortDistributionService: 'CohortDistributionDataService',
  participantManagementService: 'ParticipantManagementDataService',
  exceptionManagementService: 'ExceptionManagementDataService',
  participantDemographicDataService: 'ParticipantDemographicDataService',
  serviceNowCasesDataService: 'ServiceNowCasesDataService',
  createPDSDemographicPath: 'api/RetrievePdsDemographic',
  participantPayloadPath: 'src/tests/api/testFiles',
  e2eTestFilesPath: 'e2e/testFiles',
  apiTestFilesPath: 'api/testFiles',
  apiRetry: Number(process.env.API_RETRY ?? 8),
  apiWaitTime: Number(process.env.API_WAIT_MS ?? 5000),
  apiStepMs: Number(process.env.API_STEP_MS ?? 5000),
  nhsNumberKey: 'NHSNumber',
  nhsNumberKeyExceptionDemographic: 'NhsNumber',
  uniqueKeyCohortDistribution: 'CohortDistributionId',
  uniqueKeyParticipantManagement: 'ParticipantId',
  uniqueKeyParticipantDemographic: 'ParticipantId',
  uniqueKeyExceptionManagement: 'ExceptionId',
  uniqueKeyServiceNowCases: 'ServicenowId',
  ignoreValidationKey: 'apiEndpoint'
}
