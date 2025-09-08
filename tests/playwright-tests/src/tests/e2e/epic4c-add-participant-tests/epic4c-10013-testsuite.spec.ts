import { expect, test } from '../../fixtures/test-fixtures';
import { extractSubscriptionID, getRecordsFromNemsSubscription, getRecordsFromParticipantDemographicService, getRecordsFromParticipantManagementService, receiveParticipantViaServiceNow, retry } from "../../../api/distributionService/bsSelectService";
import { composeValidators, expectStatus } from "../../../api/responseValidators";
import { ParticipantRecord } from '../../../interface/InputData';
import { loadParticipantPayloads } from '../../fixtures/jsonDataReader';
import { processFileViaStorage, validateSqlDatabaseFromAPI } from '../../steps/steps';
import { TestHooks } from '../../hooks/test-hooks';

test.describe.serial('@DTOSS-3881-01 @e2e @epic4c- Verify Cohort Manager has received data amendments from PDS', () => {
  TestHooks.setupAllTestHooks();
  let participantData: Record<string, ParticipantRecord>;

  test.beforeAll(async () => {
    const folderName = '@DTOSS-3881-01';
    const fileName = 'ADD-participantPayload.json';
    participantData = await loadParticipantPayloads(folderName, fileName);
  });

  test.skip('@DTOSS-3881-01 DTOSS-10013 @not-runner-based - Verify subscription IDs on Nems table for ADD', {
    annotation: [{
      type: 'Requirement - https://nhsd-jira.digital.nhs.uk/browse/DTOSS-3881',
      description: 'Tests - https://nhsd-jira.digital.nhs.uk/browse/DTOSS-10013',
    }]
  }, async ({ request }, testInfo) => {

    await test.step('Given Cohort manager receives data from ServiceNow and subscribed to PDS', async () => {
      const payload = participantData['inputParticipantRecord10013'];
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
          res?.data?.[0]?.NhsNumber === 9996534472 &&
          res?.data?.[0]?.GivenName === "Jane" &&
          res?.data?.[0]?.FamilyName === "Doe",
        { retries: 8, delayMs: 5000 }
      );
      expect(response?.data?.[0]?.NhsNumber).toBe(9996534472);
      expect(response?.data?.[0]?.GivenName).toBe("Jane");
      expect(response?.data?.[0]?.FamilyName).toBe("Doe");
    });

    await test.step('And NHSNumber, ScreeningId, ReferralFlag is written to Participant Management table', async () => {
      const response = await retry(
        () => getRecordsFromParticipantManagementService(request),
        (res) =>
          res?.data?.[0]?.NHSNumber === 9996534472 &&
          res?.data?.[0]?.ScreeningId === 1 &&
          res?.data?.[0]?.ReferralFlag === 1,
        { retries: 8, delayMs: 5000 }
      );
      expect(response?.data?.[0]?.NHSNumber).toBe(9996534472);
      expect(response?.data?.[0]?.ScreeningId).toBe(1);
      expect(response?.data?.[0]?.ReferralFlag).toBe(1);
    });

    await test.step('DTOSS-10012 DTOSS-10012 verify NemsSubscription_Id in NEMS_SUBSCRIPTION table', async () => {
      const nhsNumber = participantData['inputParticipantRecord10013'].u_case_variable_data.nhs_number;

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
        { retries: 8, delayMs: 5000 }
      );
      const subscriptionID = extractSubscriptionID(response);
      expect(subscriptionID).not.toBeNull();
      console.log(`Extracted Subscription ID: ${subscriptionID} for number: ${nhsNumber}`);
    });
  });


  test.skip('DTOSS-10013 And send AMEND from CAS to Cohort Manager', {
    annotation: [{
      type: 'Requirement - https://nhsd-jira.digital.nhs.uk/browse/DTOSS-3881',
      description: 'Tests - https://nhsd-jira.digital.nhs.uk/browse/DTOSS-10013',
    }]
  }, async ({ request, testData }) => {

    await test.step(`When same ADD participant record is AMENDED via storage for ${testData.nhsNumberAmend}`, async () => {
      await processFileViaStorage(testData.runTimeParquetFileRoutineAmend);
    });

    await test.step(`Then the record should end up in cohort distribution table`, async () => {
      await validateSqlDatabaseFromAPI(request, testData.checkInDatabaseAmend);
    });

  });
});
