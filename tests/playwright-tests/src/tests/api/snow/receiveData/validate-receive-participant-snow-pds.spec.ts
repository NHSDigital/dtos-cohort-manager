import { test, expect, APIRequestContext } from '@playwright/test';
import { composeValidators, expectStatus } from '../../../../api/responseValidators';
import { receiveParticipantViaServiceNow, invalidServiceNowEndpoint, getRecordsFromParticipantManagementService, getRecordsFromParticipantDemographicService, getRecordsFromExceptionManagementService } from '../../../../api/distributionService/bsSelectService';
import { loadParticipantPayloads, omitField } from '../../../fixtures/jsonDataReader';
import { ParticipantRecord } from '../../../../interface/InputData';
import { ApiResponse } from '../../../../api/core/types';

test.describe.serial('@regression @service_now @api Verify Add Participant data', async () => {

  let participantData: Record<string, ParticipantRecord>;

  test.beforeAll(() => {
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

    await test.step('receiving data from serviceNow', async () => {

      const payload = participantData['validParticipantRecordOnPDS'];
      const response = await receiveParticipantViaServiceNow(request, payload);

      const validators = composeValidators(
        expectStatus(202)
      );
      await validators(response);
    });

    await test.step('Check that data is in Participant Management table', async () => {
      const response = await getRecordsFromParticipantManagementService(request);

      const validators = composeValidators(
        expectStatus(200)
      );
      await validators(response);

      expect(response.data[0].NHSNumber).toBe(9998380197);
      expect(response.data[0].ScreeningId).toBe(1);
      expect(response.data[0].ReferralFlag).toBe(1);
      expect(response.data[0].ReferralFlag).toBe(1);
      expect(response.data[0].EligibilityFlag).toBe(1);
    });

    await test.step('Check that data is in Participant Demographic table', async () => {
      const response = await getRecordsFromParticipantDemographicService(request);

      const validators = composeValidators(
        expectStatus(200)
      );
      await validators(response);

      expect(response.data[0].NhsNumber).toBe(9998380197);
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

    await test.step('receiving data from serviceNow', async () => {

      const payload = participantData['validParticipantRecordOnPDSUnhappyPath'];
      const response = await receiveParticipantViaServiceNow(request, payload);

      const validators = composeValidators(
        expectStatus(202)
      );
      await validators(response);
    });

    await test.step('Check that exception is raised in Exception Manegement table', async () => {
      const response = await getRecordsFromExceptionManagementService(request);

      const validators = composeValidators(
        expectStatus(200)
      );
      await validators(response);

      expect(response.data[0].NhsNumber).toBe("9000000009");
      expect(response.data[0].RuleId).toBe(-2146233088);
      expect(response.data[0].RuleDescription).toBe("Participant data from ServiceNow does not match participant data from PDS");
    });
  });
});

