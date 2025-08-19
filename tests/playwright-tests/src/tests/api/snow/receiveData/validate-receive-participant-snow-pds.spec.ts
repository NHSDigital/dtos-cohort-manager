import { test, expect, APIRequestContext } from '@playwright/test';
import { composeValidators, expectStatus } from '../../../../api/responseValidators';
import { receiveParticipantViaServiceNow, getRecordsFromParticipantManagementService, getRecordsFromParticipantDemographicService, getRecordsFromExceptionManagementService } from '../../../../api/distributionService/bsSelectService';
import { loadParticipantPayloads, omitField } from '../../../fixtures/jsonDataReader';
import { ParticipantRecord } from '../../../../interface/InputData';
import { cleanupDatabaseFromAPI } from '../../../steps/steps';

test.describe.serial('@regression @service_now @api @not-runner-based Verify Add Participant data', async () => {

  let participantData: Record<string, ParticipantRecord>;

  const testNumbers = ["9993773360", "9998695511"];

  test.beforeAll(async ({ request }) => {
    await cleanupDatabaseFromAPI(request, testNumbers);
    const folderName = '@DTOSS-8375-01';
    const fileName = 'ADD-participantPayload.json';
    participantData = loadParticipantPayloads(folderName, fileName);
  });

  test('@DTOSS-10006 Add - Verify Participant Record: Happy Path case of data match', async ({ request }) => {
    annotation: [{
      type: 'Requirement',
      description: 'Tests - https://nhsd-jira.digital.nhs.uk/browse/DTOSS-10006',
    }, {
      type: 'Requirement',
      description: 'Tests - https://nhsd-jira.digital.nhs.uk/browse/DTOSS-8375',
    },]

    await test.step('DTOSS-10006 - receiving data from serviceNow', async () => {

      const payload = participantData['validParticipantRecordOnPDS'];
      const response = await receiveParticipantViaServiceNow(request, payload);

      const validators = composeValidators(
        expectStatus(202)
      );
      await validators(response);
    });

    await test.step('DTOSS-10006 - Check that data is in Participant Management table', async () => {
      const response = await getRecordsFromParticipantManagementService(request);

      const validators = composeValidators(
        expectStatus(200)
      );
      await validators(response);

      expect(response.data[0].NHSNumber).toBe(9993773360);
      expect(response.data[0].ScreeningId).toBe(1);
      expect(response.data[0].ReferralFlag).toBe(1);
      expect(response.data[0].ReferralFlag).toBe(1);
      expect(response.data[0].EligibilityFlag).toBe(1);
    });

    await test.step('DTOSS-10006 - Check that data is in Participant Demographic table', async () => {
      const response = await getRecordsFromParticipantDemographicService(request);

      const validators = composeValidators(
        expectStatus(200)
      );
      await validators(response);

      expect(response.data[0].NhsNumber).toBe(9993773360);
      expect(response.data[0].GivenName).toBe("Jane");
      expect(response.data[0].FamilyName).toBe("Doe");
    });
  });


  test('@DTOSS-10007 Add - Verify exception is raised for record mismatch between snow and pds', async ({ request }) => {
    annotation: [{
      type: 'Requirement',
      description: 'Tests - https://nhsd-jira.digital.nhs.uk/browse/DTOSS-10007',
    }, {
      type: 'Requirement',
      description: 'Tests - https://nhsd-jira.digital.nhs.uk/browse/DTOSS-8375',
    },]

    await test.step('DTOSS-10007 - receiving data from serviceNow ', async () => {

      const payload = participantData['validParticipantRecordOnPDSUnhappyPath'];
      const response = await receiveParticipantViaServiceNow(request, payload);

      const validators = composeValidators(
        expectStatus(202)
      );
      await validators(response);
    });

    await test.step('DTOSS-10007 - Check that exception status', async () => {
      const response = await getRecordsFromExceptionManagementService(request);

      const validators = composeValidators(
        expectStatus(204)
      );
      await validators(response);
    });
  });
});
