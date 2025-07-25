import { test, expect, APIRequestContext } from '@playwright/test';
import { composeValidators, expectStatus } from '../../../../api/responseValidators';
import { receiveParticipantViaServiceNow } from '../../../../api/distributionService/bsSelectService';
import { getAllParticipantPayloads, omitField } from '../../../fixtures/jsonDataReader';

test.describe.serial('@service_now @regression @api receive valid participant from serviceNow', () => {
  const payloads = getAllParticipantPayloads();

  test('@DTOSS-3880 - Add a valid VHR participant successfully', async ({ request }) => {
    const payload = payloads['validParticipantRecord-vhr'];
    const response = await receiveParticipantViaServiceNow(request, payload);

    const validators = composeValidators(
      expectStatus(202)
    );
    await validators(response);
  });

  test('@DTOSS-3880 - Add a valid CEASED participant successfully', async ({ request }) => {
    const payload = payloads['validParticipantRecord-ceasing'];
    const response = await receiveParticipantViaServiceNow(request, payload);

    const validators = composeValidators(
      expectStatus(202)
    );
    await validators(response);
  });

  test('@DTOSS-3880 - Add a valid ROUTINE participant successfully', async ({ request }) => {
    const payload = payloads['validParticipantRecord-routine'];
    const response = await receiveParticipantViaServiceNow(request, payload);

    const validators = composeValidators(
      expectStatus(202)
    );
    await validators(response);
  });

  test('@DTOSS-3880 - Return error when mandatory participant data attribute is missing', async ({ request }) => {
    const payload = payloads['inValidParticipantRecordMissingDateofBirthAndSurname'];
    const response = await receiveParticipantViaServiceNow(request, payload);

    const validators = composeValidators(
      expectStatus(400)
    );
    await validators(response);
  });

  test('@DTOSS-3880 - Return error when an invalid field name is received', async ({ request }) => {
    const payload = payloads['invalidFieldsName'];
    const response = await receiveParticipantViaServiceNow(request, payload);

    const validators = composeValidators(
      expectStatus(400)
    );
    await validators(response);
  });

  test('@DTOSS-3880 - No error when dummy code is blank', async ({ request }) => {
    const payload = payloads['ValidParticipantWithNoDummyCode'];
    const response = await receiveParticipantViaServiceNow(request, payload);

    const validators = composeValidators(
      expectStatus(202)
    );
    await validators(response);
  });

  test('@DTOSS-3880 - participant with missing mandatory field is rejected', async ({ request }) => {
    const originalPayload = payloads['validParticipantRecord-vhr'];
    const deleteAField = omitField(originalPayload, 'u_case_variable_data.date_of_birth');
    const response = await receiveParticipantViaServiceNow(request, deleteAField);

    const validators = composeValidators(
      expectStatus(400)
    );
    await validators(response);
  });
});
