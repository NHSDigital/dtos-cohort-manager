import { test, expect, APIRequestContext } from '@playwright/test';
import { composeValidators, expectStatus } from '../../../../api/responseValidators';
import { receiveParticipantViaServiceNow } from '../../../../api/distributionService/bsSelectService';
import { loadParticipantPayloads, omitField } from '../../../fixtures/jsonDataReader';
import { ParticipantRecord } from '../../../../interface/InputData';


test.describe.serial('@regression @service_now @api receive valid participant from serviceNow api', () => {

  let participantData: Record<string, ParticipantRecord>;

  test.beforeAll(() => {
    const folderName = '@DTOSS-3880-01';
    const fileName = 'ADD-participantPayload.json';
    participantData = loadParticipantPayloads(folderName, fileName);
  });

  test('@DTOSS-3880 @DTOSS-8424 Add a valid VHR participant successfully', async ({ request }) => {
    annotation: [{
      type: 'Requirement',
      description: 'Tests - https://nhsd-jira.digital.nhs.uk/browse/DTOSS-3880',
    }, {
      type: 'Requirement',
      description: 'Tests - https://nhsd-jira.digital.nhs.uk/browse/DTOSS-8424',
    },]

    const payload = participantData['validParticipantRecord-vhr'];
    const response = await receiveParticipantViaServiceNow(request, payload);

    const validators = composeValidators(
      expectStatus(202)
    );
    await validators(response);
  });

  test('@DTOSS-3880 @DTOSS-8424 Add a valid CEASED participant successfully', async ({ request }) => {

    annotation: [{
      type: 'Requirement',
      description: 'Tests - https://nhsd-jira.digital.nhs.uk/browse/DTOSS-3880',
    }, {
      type: 'Requirement',
      description: 'Tests - https://nhsd-jira.digital.nhs.uk/browse/DTOSS-8424',
    },]

    const payload = participantData['validParticipantRecord-ceasing'];
    const response = await receiveParticipantViaServiceNow(request, payload);

    const validators = composeValidators(
      expectStatus(202)
    );
    await validators(response);
  });

  test('@DTOSS-3880 @DTOSS-8424 Add a valid ROUTINE participant successfully', async ({ request }) => {

    annotation: [{
      type: 'Requirement',
      description: 'Tests - https://nhsd-jira.digital.nhs.uk/browse/DTOSS-3880',
    }, {
      type: 'Requirement',
      description: 'Tests - https://nhsd-jira.digital.nhs.uk/browse/DTOSS-8424',
    },]

    const payload = participantData['validParticipantRecord-routine'];
    const response = await receiveParticipantViaServiceNow(request, payload);

    const validators = composeValidators(
      expectStatus(202)
    );
    await validators(response);
  });

  test('@DTOSS-3880 @DTOSS-8424 Return error when mandatory participant data attribute is missing', async ({ request }) => {

    annotation: [{
      type: 'Requirement',
      description: 'Tests - https://nhsd-jira.digital.nhs.uk/browse/DTOSS-3880',
    }, {
      type: 'Requirement',
      description: 'Tests - https://nhsd-jira.digital.nhs.uk/browse/DTOSS-8424',
    },]

    const payload = participantData['inValidParticipantRecordMissingDateofBirthAndSurname'];
    const response = await receiveParticipantViaServiceNow(request, payload);

    const validators = composeValidators(
      expectStatus(400)
    );
    await validators(response);
  });

    test('@DTOSS-3880 @DTOSS-8424 Return error when all mandatory participant data attribute is missing', async ({ request }) => {

    annotation: [{
      type: 'Requirement',
      description: 'Tests - https://nhsd-jira.digital.nhs.uk/browse/DTOSS-3880',
    }, {
      type: 'Requirement',
      description: 'Tests - https://nhsd-jira.digital.nhs.uk/browse/DTOSS-8424',
    },]

    const payload = participantData['ParticipantRecordMissingAllMandatoryData'];
    const response = await receiveParticipantViaServiceNow(request, payload);

    const validators = composeValidators(
      expectStatus(400)
    );
    await validators(response);
  });

  test('@DTOSS-3880 @DTOSS-8424 Return error when mandatory reason for add is empty', async ({ request }) => {

    annotation: [{
      type: 'Requirement',
      description: 'Tests - https://nhsd-jira.digital.nhs.uk/browse/DTOSS-3880',
    }, {
      type: 'Requirement',
      description: 'Tests - https://nhsd-jira.digital.nhs.uk/browse/DTOSS-8424',
    },]

    const payload = participantData['inValidParticipantRecordMissingReasonForAdding'];
    const response = await receiveParticipantViaServiceNow(request, payload);

    const validators = composeValidators(
      expectStatus(400)
    );
    await validators(response);
  });

  test('@DTOSS-3880 @DTOSS-8424 Return error when an invalid schema name is received', async ({ request }) => {

    annotation: [{
      type: 'Requirement',
      description: 'Tests - https://nhsd-jira.digital.nhs.uk/browse/DTOSS-3880',
    }, {
      type: 'Requirement',
      description: 'Tests - https://nhsd-jira.digital.nhs.uk/browse/DTOSS-8424',
    },]

    const payload = participantData['invalidSchemaName'];
    const response = await receiveParticipantViaServiceNow(request, payload);

    const validators = composeValidators(
      expectStatus(400)
    );
    await validators(response);
  });

  test('@DTOSS-3880 @DTOSS-8424 No error when dummy code is blank', async ({ request }) => {

    annotation: [{
      type: 'Requirement',
      description: 'Tests - https://nhsd-jira.digital.nhs.uk/browse/DTOSS-3880',
    }, {
      type: 'Requirement',
      description: 'Tests - https://nhsd-jira.digital.nhs.uk/browse/DTOSS-8424',
    },]

    const payload = participantData['ValidParticipantWithNoDummyCode'];
    const response = await receiveParticipantViaServiceNow(request, payload);

    const validators = composeValidators(
      expectStatus(202)
    );
    await validators(response);
  });

  test('@DTOSS-3880 @DTOSS-8424 participant with missing mandatory field is rejected', async ({ request }) => {

    annotation: [{
      type: 'Requirement',
      description: 'Tests - https://nhsd-jira.digital.nhs.uk/browse/DTOSS-3880',
    }, {
      type: 'Requirement',
      description: 'Tests - https://nhsd-jira.digital.nhs.uk/browse/DTOSS-8424',
    },]

    const originalPayload = participantData['validParticipantRecord-vhr'];
    const deleteAField = omitField(originalPayload, 'u_case_variable_data.date_of_birth');

    const response = await receiveParticipantViaServiceNow(request, deleteAField as ParticipantRecord);

    const validators = composeValidators(
      expectStatus(400)
    );
    await validators(response);
  });
});
