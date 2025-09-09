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
      let response;
      let success = false;

      for (let i = 0; i < 10; i++) {
        response = await receiveParticipantViaServiceNow(request, payload);
        if (response.status === 202) {
          success = true;
          break;
        }
        console.log(`Waiting for succesful data receival from serviceNow... (${i + 1}/10)`);
        await new Promise(res => setTimeout(res, 5000));
      }
      if (!success) {
        throw new Error('Cohort Manager receive data was unsuccessful after 10 retries (20 seconds).');
      }
    });

    await test.step('Then NHSNumber, GivenName, FamilyName is written to Participant Demographic table', async () => {
      let response;
      let success = false;

      for (let i = 0; i < 10; i++) {
        response = await getRecordsFromParticipantDemographicDataService(request);
        if (response.status === 200) {
          success = true;
          break;
        }
        console.log(`Waiting for participant response data to be available... (${i + 1}/10)`);
        await new Promise(res => setTimeout(res, 5000));
      }
      if (!success) {
        throw new Error('Participant response data was not available after 10 retries (20 seconds).');
      }
      expect(response?.data?.[0]?.NhsNumber).toBe(9997160908);
      expect(response?.data?.[0]?.GivenName).toBe(givenName);
      expect(response?.data?.[0]?.FamilyName).toBe(familyName);
    });

    await test.step('And NHSNumber, ScreeningId, ReferralFlag is written to Participant Management table', async () => {
      let response;
      let success = false;
      let found = false;

      const isMatchingRow = (row: { NHSNumber: number; ScreeningId: number; ReferralFlag: number }): boolean =>
        row.NHSNumber === 9997160908 && row.ScreeningId === 1 && row.ReferralFlag === 1;

      for (let i = 0; i < 10; i++) {
        response = await getRecordsFromParticipantManagementDataService(request);

        if (response.status === 200) {
          success = true;
          found = response.data.some(isMatchingRow);
          if (found) {
            const match = response.data.find(isMatchingRow);
            console.log(`Found matching Number: ${match.NHSNumber}`);
            break;
          }
        }
        console.log(`Waiting for participant response data to be available... (${i + 1}/10)`);
        await new Promise(res => setTimeout(res, 5000));
      }
      if (!success) {
        throw new Error('Participant response data was not available after 10 retries (50 seconds).');
      }
      if (!found) {
        throw new Error('Matching participant record was not found after 10 retries (50 seconds).');
      }
    });

    await test.step('DTOSS-10012 DTOSS-10012 verify NemsSubscription_Id in NEMS_SUBSCRIPTION table', async () => {
      let response: any;
      let success = false;

      for (let i = 0; i < 10; i++) {
        response = await getRecordsFromNemsSubscription(request, nhsNumber);
        if (response?.status === 200) {
          success = true;
          break;
        }
        console.log(`Waiting for NemsSubscription_Id data to be available... (${i + 1}/10)`);
        await new Promise(res => setTimeout(res, 5000));
      }
      if (!success || !response) {
        throw new Error('NemsSubscription_Id response data was not available after 10 retries (20 seconds).');
      }
      const subscriptionID = extractSubscriptionID(response);
      expect(subscriptionID).not.toBeNull();
      console.log(`Extracted Subscription ID: ${subscriptionID} for number: ${nhsNumber}`);
    });
  });
});
