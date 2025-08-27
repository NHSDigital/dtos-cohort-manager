import { test } from '@playwright/test';
import { composeValidators, expectStatus, validateResponseByStatus } from '../../../../api/responseValidators';
import { receiveParticipantViaServiceNow } from '../../../../api/distributionService/bsSelectService';
import { loadParticipantPayloads } from '../../../fixtures/jsonDataReader';
import { validateSqlDatabaseFromAPI } from '../../../steps/steps';

test.describe('@regression @service_now @api DTOSS-10026 system error pathway', () => {
  let participantData: Record<string, any>;

  test.beforeAll(() => {
    const folderName = '@DTOSS-8569-01';
    const fileName = 'ADD-participantPayload.json';
    participantData = loadParticipantPayloads(folderName, fileName);
  });

  test('@DTOSS-10026-01 Exception is raised when record fails due to system failure (PDS error)', async ({ request }) => {
    const payload = participantData['validParticipantRecord-ceasing'];

    const response = await receiveParticipantViaServiceNow(request, payload);
    const validators = composeValidators(expectStatus(202), validateResponseByStatus());
    await validators(response);

    const nhsNumber = payload.u_case_variable_data.nhs_number;
    const validations = [
      {
        validations: {
          apiEndpoint: 'api/ExceptionManagementDataService',
          NhsNumber: String(nhsNumber),
          expectedCount: 1,
        },
        meta: {
          testJiraId: '@DTOSS-10026-01',
          requirementJiraId: 'DTOSS-8569',
          additionalTags: '@regression @service_now @api Exception path: exception raised',
        },
      },
    ];

    await validateSqlDatabaseFromAPI(request, validations);
  });
});
