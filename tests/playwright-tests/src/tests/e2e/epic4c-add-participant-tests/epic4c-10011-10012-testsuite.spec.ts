import { expect, test } from '../../fixtures/test-fixtures';
import { extractSubscriptionID, getRecordsFromNemsSubscription, getRecordsFromParticipantDemographicService, getRecordsFromParticipantManagementService, receiveParticipantViaServiceNow, retry } from "../../../api/distributionService/bsSelectService";
import { composeValidators, expectStatus } from "../../../api/responseValidators";
import { ParticipantRecord } from '../../../interface/InputData';
import { loadParticipantPayloads } from '../../fixtures/jsonDataReader';

test.describe.serial('@DTOSS-3881-01 @e2e @epic4c- Cohort Manger subscribed the Added record with PDS', () => {

  let participantData: Record<string, ParticipantRecord>;

  test.beforeAll(async () => {
    const folderName = '@DTOSS-3881-01';
    const fileName = 'ADD-participantPayload.json';
    participantData = await loadParticipantPayloads(folderName, fileName);
  });

  test('@DTOSS-3881-01 DTOSS-10011 @not-runner-based - Verify subscription IDs on Nems table for ADD', {
    annotation: [{
      type: 'Requirement - https://nhsd-jira.digital.nhs.uk/browse/DTOSS-3881',
      description: 'Tests - https://nhsd-jira.digital.nhs.uk/browse/DTOSS-10011',
    }]
  }, async ({ request }, testInfo) => {

    await test.step('Given Cohort manager receives data from ServiceNow and subscribed to PDS', async () => {
      const payload = participantData['inputParticipantRecord'];
      const response = await receiveParticipantViaServiceNow(request, payload);
      const validators = composeValidators(
        expectStatus(202)
      );
      await validators(response);
    });

    await test.step('Then NHSNumber, GivenName, FamilyName is written to Participant Demographic table', async () => {
      const response = await retry(
        () => getRecordsFromParticipantDemographicService(request),
        (res) =>
          res?.data?.[0]?.NhsNumber === 9997160908 &&
          res?.data?.[0]?.GivenName === "Jane" &&
          res?.data?.[0]?.FamilyName === "Doe",
        { retries: 5, delayMs: 2000 }
      );
      expect(response?.data?.[0]?.NhsNumber).toBe(9997160908);
      expect(response?.data?.[0]?.GivenName).toBe("Jane");
      expect(response?.data?.[0]?.FamilyName).toBe("Doe");
    });

    await test.step('And NHSNumber, ScreeningId, ReferralFlag is written to Participant Management table', async () => {
      const response = await retry(
        () => getRecordsFromParticipantManagementService(request),
        (res) =>
          res?.data?.[0]?.NHSNumber === 9997160908 &&
          res?.data?.[0]?.ScreeningId === 1 &&
          res?.data?.[0]?.ReferralFlag === 1,
        { retries: 5, delayMs: 2000 }
      );
      expect(response?.data?.[0]?.NHSNumber).toBe(9997160908);
      expect(response?.data?.[0]?.ScreeningId).toBe(1);
      expect(response?.data?.[0]?.ReferralFlag).toBe(1);
    });

    await test.step('DTOSS-10012 DTOSS-10012 verify NemsSubscription_Id in NEMS_SUBSCRIPTION table', async () => {
      const nhsNumber = participantData['inputParticipantRecord'].u_case_variable_data.nhs_number;

      const response = await retry(
        async () => {
          const res = await getRecordsFromNemsSubscription(request, nhsNumber);
          const validators = composeValidators(expectStatus(200));
          await validators(res);

          return res;
        },
        (res) => {
          const subscriptionID = extractSubscriptionID(res);
          return subscriptionID !== null;
        },
        { retries: 5, delayMs: 2000 }
      );
      const subscriptionID = extractSubscriptionID(response);
      expect(subscriptionID).not.toBeNull();
      console.log(`Extracted Subscription ID: ${subscriptionID} for number: ${nhsNumber}`);
    });
  });
});
