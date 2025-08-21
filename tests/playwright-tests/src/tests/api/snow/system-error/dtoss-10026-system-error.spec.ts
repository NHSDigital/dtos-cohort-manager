import { test } from '@playwright/test';
import { composeValidators, expectStatus } from '../../../../api/responseValidators';
import { invalidServiceNowEndpoint } from '../../../../api/distributionService/bsSelectService';
import { loadParticipantPayloads } from '../../../fixtures/jsonDataReader';

test.describe('@regression @service_now @api DTOSS-10026 system error pathway', () => {
  let participantData: Record<string, any>;

  test.beforeAll(() => {
    const folderName = '@DTOSS-8569-01';
    const fileName = 'ADD-participantPayload.json';
    participantData = loadParticipantPayloads(folderName, fileName);
  });

  test('@DTOSS-10026-01 System error when ServiceNow endpoint invalid', async ({ request }) => {
    const payload = participantData['validParticipantRecord-vhr'];
    const response = await invalidServiceNowEndpoint(request, payload);
    const validators = composeValidators(expectStatus(404));
    await validators(response);
  });
});
