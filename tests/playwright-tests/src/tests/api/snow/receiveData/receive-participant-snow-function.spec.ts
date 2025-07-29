import { test, expect, APIRequestContext } from '@playwright/test';
import { composeValidators, expectStatus } from '../../../../api/responseValidators';
import { receiveParticipantViaServiceNow } from '../../../../api/distributionService/bsSelectService';
import { loadParticipantPayloads , omitField } from '../../../fixtures/jsonDataReader';
import { ParticipantRecord } from '../../../../interface/InputData';


test.describe.serial('@regression @service_now @api receive valid participant from serviceNow api', () => {

  let participantData: Record<string, ParticipantRecord>;

  test.beforeAll(() => {
    const folderName = '@DTOSS-3880-01';
    const fileName = 'ADD-participantPayload.json';
    participantData = loadParticipantPayloads(folderName, fileName);
  });

  test('@DTOSS-3880 - Add a valid VHR participant successfully', async ({ request }) => {
    const payload = participantData['validParticipantRecord-vhr'];
    const response = await receiveParticipantViaServiceNow(request, payload);

    const validators = composeValidators(
      expectStatus(202)
    );
    await validators(response);
  });

  test('@DTOSS-3880 - Add a valid CEASED participant successfully', async ({ request }) => {
    const payload = participantData['validParticipantRecord-ceasing'];
    const response = await receiveParticipantViaServiceNow(request, payload);

    const validators = composeValidators(
      expectStatus(202)
    );
    await validators(response);
  });

  test('@DTOSS-3880 - Add a valid ROUTINE participant successfully', async ({ request }) => {
    const payload = participantData['validParticipantRecord-routine'];
    const response = await receiveParticipantViaServiceNow(request, payload);

    const validators = composeValidators(
      expectStatus(202)
    );
    await validators(response);
  });

  test('@DTOSS-3880 - Return error when mandatory participant data attribute is missing', async ({ request }) => {
    const payload = participantData['inValidParticipantRecordMissingDateofBirthAndSurname'];
    const response = await receiveParticipantViaServiceNow(request, payload);

    const validators = composeValidators(
      expectStatus(400)
    );
    await validators(response);
  });

  test('@DTOSS-3880 - Return error when an invalid field name is received', async ({ request }) => {
    const payload = participantData['invalidFieldsName'];
    const response = await receiveParticipantViaServiceNow(request, payload);

    const validators = composeValidators(
      expectStatus(400)
    );
    await validators(response);
  });

  test('@DTOSS-3880 - No error when dummy code is blank', async ({ request }) => {
    const payload = participantData['ValidParticipantWithNoDummyCode'];
    const response = await receiveParticipantViaServiceNow(request, payload);

    const validators = composeValidators(
      expectStatus(202)
    );
    await validators(response);
  });

  test('@DTOSS-3880 - participant with missing mandatory field is rejected', async ({ request }) => {
    const originalPayload = participantData['validParticipantRecord-vhr'];
    const deleteAField = omitField(originalPayload, 'u_case_variable_data.date_of_birth');

    const response = await receiveParticipantViaServiceNow(request, deleteAField as ParticipantRecord);

    const validators = composeValidators(
      expectStatus(400)
    );
    await validators(response);
  });
});
