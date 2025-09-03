import { expect, test } from '../../fixtures/test-fixtures';
import { extractSubscriptionID, getRecordsFromNemsSubscription, getRecordsFromParticipantDemographicService, getRecordsFromParticipantManagementService, receiveParticipantViaServiceNow } from "../../../api/distributionService/bsSelectService";
import { composeValidators, expectStatus } from "../../../api/responseValidators";
import { ParticipantRecord } from '../../../interface/InputData';
import { loadParticipantPayloads } from '../../fixtures/jsonDataReader';

test.describe('@DTOSS-3881-01 @e2e @epic4c- Cohort Manger subscribed the Added record with PDS', () => {

  let participantData: Record<string, ParticipantRecord>;

  test.beforeAll(() => {
    const folderName = '@DTOSS-3881-01';
    const fileName = 'ADD-participantPayload.json';
    participantData = loadParticipantPayloads(folderName, fileName);
  });

  test('@DTOSS-3881-01 DTOSS-10011 @not-runner-based - Verify subscription IDs on Nems table for ADD', {
    annotation: [{
      type: 'Requirement - https://nhsd-jira.digital.nhs.uk/browse/DTOSS-3881',
      description: 'Tests - https://nhsd-jira.digital.nhs.uk/browse/DTOSS-10011',
    }]
  }, async ({ request }, testInfo) => {

    await test.step('Given Cohort manager receives data from ServiceNow and subscribed to PDS', async () => {
      const payload = participantData['inputParticipantRecord_test'];
      const response = await receiveParticipantViaServiceNow(request, payload);
      const validators = composeValidators(
        expectStatus(202)
      );
      await validators(response);
    });

    await test.step('Then NHSNumber, GivenName, FamilyName is written to Participant Demographic table', async () => {
      const response = await getRecordsFromParticipantDemographicService(request);

      expect(response?.data?.[0]?.NhsNumber).toBe(9997160908);
      expect(response?.data?.[0]?.GivenName).toBe("Jane");
      expect(response?.data?.[0]?.FamilyName).toBe("Doe");
    });

    await test.step('And NHSNumber, ScreeningId, ReferralFlag is written to Participant Management table', async () => {
      const response = await getRecordsFromParticipantManagementService(request);
      expect(response?.data?.[0]?.NHSNumber).toBe(9997160908);
      expect(response?.data?.[0]?.ScreeningId).toBe(1);
      expect(response?.data?.[0]?.ReferralFlag).toBe(1);
    });

    await test.step('Assert that NemsSubscription service is available', async () => {
      const nhsNumber = participantData['inputParticipantRecord_test'].u_case_variable_data.nhs_number;
      const response = await getRecordsFromNemsSubscription(request, nhsNumber);
      const validators = composeValidators(
        expectStatus(200)
      );
      await validators(response);
    });

    await test.step(`DTOSS-10012 ADD verify NemsSubscription in NEMS_SUBSCRIPTION table`, async () => {
      const nhsNumber = participantData['inputParticipantRecord_test'].u_case_variable_data.nhs_number;

      const response = await getRecordsFromNemsSubscription(request, nhsNumber);
      const subscriptionID = extractSubscriptionID(response);
      expect(response.status).toBe(200);
      expect(subscriptionID).not.toBeNull();

      console.log(`Extracted Subscription ID: ${subscriptionID} for NHS number: ${nhsNumber}`);
    });
  });

  test('@DTOSS-3881-02 DTOSS-10013 @not-runner-based AMEND verify NemsSubscription_id in NEMS_SUBSCRIPTION table', {
    annotation: [{
      type: 'Requirement - https://nhsd-jira.digital.nhs.uk/browse/DTOSS-3881',
      description: 'Tests - https://nhsd-jira.digital.nhs.uk/browse/DTOSS-10013',
    }]
  }, async ({ request }, testInfo) => {

      const nhsNumber = '9996534472';

      const response = await getRecordsFromNemsSubscription(request, nhsNumber);
      const subscriptionID = extractSubscriptionID(response);
      expect(response.status).toBe(200);
      expect(subscriptionID).not.toBeNull();

      console.log(`Extracted Subscription ID: ${subscriptionID} for NHS number: ${nhsNumber}`);
  });
});
