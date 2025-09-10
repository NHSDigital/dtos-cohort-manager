import { test} from '@playwright/test';
import { composeValidators, expectStatus } from '../../../api/responseValidators';
import { receiveParticipantViaServiceNow, invalidServiceNowEndpoint } from '../../../api/distributionService/bsSelectService';
import { loadParticipantPayloads } from '../../fixtures/jsonDataReader';
import { ParticipantRecord } from '../../../interface/InputData';


test.describe.serial('@DTOSS-3880 @epic4c- @api @not-runner-based receive valid participant from serviceNow api', () => {
  let participantData: Record<string, ParticipantRecord>;

  test.beforeAll(async () => {
    const folderName = '@DTOSS-3880-01';
    const fileName = 'ADD-participantPayload.json';
    participantData = await loadParticipantPayloads(folderName, fileName);
  });

  test('@DTOSS-3880 @DTOSS-8424 1. Add a valid VHR participant successfully', async ({ request }, testInfo) => {
    testInfo.annotations.push({
      type: 'Requirement - https://nhsd-jira.digital.nhs.uk/browse/DTOSS-3880',
      description: 'Tests - https://nhsd-jira.digital.nhs.uk/browse/DTOSS-8424',
    });

    const payload = participantData['validParticipantRecord-vhr'];

    await test.step('Given a valid vhr participant is received, then response code is 202', async () => {
      const response = await receiveParticipantViaServiceNow(request, payload);
      await new Promise(res => setTimeout(res, 2000));
      const validators = composeValidators(
        expectStatus(202)
      );
      await validators(response);
    });
  });

  test('@DTOSS-3880 @DTOSS-8424 2. Add a valid CEASED participant successfully', async ({ request }, testInfo) => {
    testInfo.annotations.push({
      type: 'Requirement - https://nhsd-jira.digital.nhs.uk/browse/DTOSS-3880',
      description: 'Tests - https://nhsd-jira.digital.nhs.uk/browse/DTOSS-8424',
    });

    const payload = participantData['validParticipantRecord-ceasing'];

    await test.step('Given a valid ceasing participant is received, then response code is 202', async () => {
      const response = await receiveParticipantViaServiceNow(request, payload);
      const validators = composeValidators(
        expectStatus(202)
      );
      await validators(response);
    });
  });

  test('@DTOSS-3880 @DTOSS-8424 3. Add a valid ROUTINE participant successfully', async ({ request }, testInfo) => {
    testInfo.annotations.push({
      type: 'Requirement - https://nhsd-jira.digital.nhs.uk/browse/DTOSS-3880',
      description: 'Tests - https://nhsd-jira.digital.nhs.uk/browse/DTOSS-8424',
    });

    const payload = participantData['validParticipantRecord-routine'];

    await test.step('Given a valid ROUTINE participant is received, then response code is 202', async () => {
      const response = await receiveParticipantViaServiceNow(request, payload);

      const validators = composeValidators(
        expectStatus(202)
      );
      await validators(response);
    });
  });

  test('@DTOSS-3880 @DTOSS-8424 4. Return error when mandatory participant data attribute is missing', async ({ request }, testInfo) => {
    testInfo.annotations.push({
      type: 'Requirement - https://nhsd-jira.digital.nhs.uk/browse/DTOSS-3880',
      description: 'Tests - https://nhsd-jira.digital.nhs.uk/browse/DTOSS-8424',
    });

    const payload = participantData['inValidParticipantRecordMissingDateofBirthAndSurname'];

    await test.step('Given a mandatory participant data is missing, then response code is 400', async () => {
      const response = await receiveParticipantViaServiceNow(request, payload);

      const validators = composeValidators(
        expectStatus(400)
      );
      await validators(response);
    });
  });

  test('@DTOSS-3880 @DTOSS-8424 5. Return error when all mandatory participant data attribute is missing', async ({ request }, testInfo) => {
    testInfo.annotations.push({
      type: 'Requirement - https://nhsd-jira.digital.nhs.uk/browse/DTOSS-3880',
      description: 'Tests - https://nhsd-jira.digital.nhs.uk/browse/DTOSS-8424',
    });

    const payload = participantData['ParticipantRecordMissingAllMandatoryData'];

    await test.step('Given all mandatory participant data is missing, then response code is 400', async () => {
      const response = await receiveParticipantViaServiceNow(request, payload);
      const validators = composeValidators(
        expectStatus(400)
      );
      await validators(response);
    });
  });

  test('@DTOSS-3880 @DTOSS-8424 6. Return error when mandatory reason for add is empty', async ({ request }, testInfo) => {
    testInfo.annotations.push({
      type: 'Requirement - https://nhsd-jira.digital.nhs.uk/browse/DTOSS-3880',
      description: 'Tests - https://nhsd-jira.digital.nhs.uk/browse/DTOSS-8424',
    });

    const payload = participantData['inValidParticipantRecordMissingReasonForAdding'];

    await test.step('Given mandatory reason for add is empty, then response code is 400', async () => {
      const response = await receiveParticipantViaServiceNow(request, payload);
      const validators = composeValidators(
        expectStatus(400)
      );
      await validators(response);
    });
  });

  test('@DTOSS-3880 @DTOSS-8424 7. Return error when an invalid schema name is received', async ({ request }, testInfo) => {
    testInfo.annotations.push({
      type: 'Requirement - https://nhsd-jira.digital.nhs.uk/browse/DTOSS-3880',
      description: 'Tests - https://nhsd-jira.digital.nhs.uk/browse/DTOSS-8424',
    });

    const payload = participantData['invalidSchemaName'];

    await test.step('Given an invalid schema is received, then response code is 400', async () => {
      const response = await receiveParticipantViaServiceNow(request, payload);
      const validators = composeValidators(
        expectStatus(400)
      );
      await validators(response);
    });
  });

  test('@DTOSS-3880 @DTOSS-8424 8. No error when dummy code is blank', async ({ request }, testInfo) => {
    testInfo.annotations.push({
      type: 'Requirement - https://nhsd-jira.digital.nhs.uk/browse/DTOSS-3880',
      description: 'Tests - https://nhsd-jira.digital.nhs.uk/browse/DTOSS-8424',
    });

    const payload = participantData['ValidParticipantWithNoDummyCode'];

    await test.step('Given a blank dummy code is received, then response code is 202', async () => {
      const response = await receiveParticipantViaServiceNow(request, payload);
      const validators = composeValidators(
        expectStatus(202)
      );
      await validators(response);
    });
  });

  test('@DTOSS-3880 @DTOSS-8424 9. participant with missing mandatory field is rejected', async ({ request }, testInfo) => {
    testInfo.annotations.push({
      type: 'Requirement - https://nhsd-jira.digital.nhs.uk/browse/DTOSS-3880',
      description: 'Tests - https://nhsd-jira.digital.nhs.uk/browse/DTOSS-8424',
    });

    const invalidDateofBirth = participantData['inValidParticipantRecordDateofBirth'];

    await test.step('Given a participant with missing mandatory field is received, then response code is 400', async () => {
      const response = await receiveParticipantViaServiceNow(request, invalidDateofBirth);
      const validators = composeValidators(
        expectStatus(400)
      );
      await validators(response);
    });
  });

  test('@DTOSS-3880 @DTOSS-8424 10. return error 404 when calling an invalid endpoint or resource', async ({ request }, testInfo) => {
    testInfo.annotations.push({
      type: 'Requirement - https://nhsd-jira.digital.nhs.uk/browse/DTOSS-3880',
      description: 'Tests - https://nhsd-jira.digital.nhs.uk/browse/DTOSS-8424',
    });

    const payload = participantData['validParticipantRecord-vhr'];

    await test.step('Given an is invalid, then response code is 404', async () => {
      const response = await invalidServiceNowEndpoint(request, payload);
      const validators = composeValidators(
        expectStatus(404)
      );
      await validators(response);
    });
  });
});
