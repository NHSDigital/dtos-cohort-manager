import { expect, test } from '../../fixtures/test-fixtures';
import { extractSubscriptionID, getRecordsFromNemsSubscription, getRecordsFromParticipantDemographicDataService, getRecordsFromParticipantManagementDataService, receiveParticipantViaServiceNow } from "../../../api/distributionService/bsSelectService";
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

  test('@DTOSS-3881-01 DTOSS-10011 @not-runner-based - Verify subscription IDs on Nems table for ADD', async ({ request }, testInfo) => {
    testInfo.annotations.push({
      type: 'Requirement - https://nhsd-jira.digital.nhs.uk/browse/DTOSS-3881',
      description: 'Tests - https://nhsd-jira.digital.nhs.uk/browse/DTOSS-10011',
    });

    const payload = participantData['inputParticipantRecord'];
    const nhsNumber = participantData['inputParticipantRecord'].u_case_variable_data.nhs_number;
    const givenName = participantData['inputParticipantRecord'].u_case_variable_data.forename_;
    const familyName = participantData['inputParticipantRecord'].u_case_variable_data.surname_family_name;

    await test.step('Given Cohort manager receives data from ServiceNow and subscribed to PDS', async () => {
      const response = await receiveParticipantViaServiceNow(request, payload);
      await new Promise(res => setTimeout(res, 2000));
      const validators = composeValidators(
        expectStatus(202)
      );
      await validators(response);
    });

    await test.step('Then NHSNumber, GivenName, FamilyName is written to Participant Demographic table', async () => {
      const response = await getRecordsFromParticipantDemographicDataService(request);
      await new Promise(res => setTimeout(res, 2000));
      const validators = composeValidators(
        expectStatus(200)
      );
      await validators(response);

      expect(response?.data?.[0]?.NhsNumber).toBe(9997160908);
      expect(response?.data?.[0]?.GivenName).toBe(givenName);
      expect(response?.data?.[0]?.FamilyName).toBe(familyName);
    });

    await test.step('And NHSNumber, ScreeningId, ReferralFlag is written to Participant Management table', async () => {
      const response = await getRecordsFromParticipantManagementDataService(request);
      await new Promise(res => setTimeout(res, 2000));
      expect(response?.data?.[0]?.NHSNumber).toBe(9997160908);
      expect(response?.data?.[0]?.ScreeningId).toBe(1);
      expect(response?.data?.[0]?.ReferralFlag).toBe(1);
    });

    await test.step('DTOSS-10012 DTOSS-10012 verify NemsSubscription_Id in NEMS_SUBSCRIPTION table', async () => {

      const response = await getRecordsFromNemsSubscription(request, nhsNumber);
      await new Promise(res => setTimeout(res, 2000));

      const validators = composeValidators(
        expectStatus(200)
      );
      await validators(response);

      const subscriptionID = extractSubscriptionID(response);
      expect(subscriptionID).not.toBeNull();
      console.log(`Extracted Subscription ID: ${subscriptionID} for number: ${nhsNumber}`);
    });
  });
});
