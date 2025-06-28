import { test, expect } from '@playwright/test';
import { createParquetFromJson } from '../../../../parquet/parquet-multiplier';
import { getApiTestData, processFileViaStorage, cleanupDatabaseFromAPI, validateSqlDatabaseFromAPI } from '../../../steps/steps';
import { getRecordsFromParticipantManagementService, BlockParticipant, UnblockParticipant} from '../../../../api/distributionService/bsSelectService'

// This test uses the previous tests' state save on code repeat
test.describe.serial(' @api Positive - The UpdateBlockFlag function called', async () => {

  test('@DTOSS-7694-01 200 @smoke @api - @TC2_SIT Verify the ability to Block a participant then Unblock them', async ({ request }, testInfo) => {

    const [checkInDatabase, inputParticipantRecord, nhsNumbers, testFilesPath] = await getApiTestData(testInfo.title);

    await cleanupDatabaseFromAPI(request, nhsNumbers);

    const parquetFile = await createParquetFromJson(nhsNumbers, inputParticipantRecord, testFilesPath);

    await test.step(`When 1 ADD participant is processed via storage`, async () => {
      await processFileViaStorage(parquetFile);
    });

    await test.step(`Then participant should be in the participant management table`, async () => {
      await validateSqlDatabaseFromAPI(request, checkInDatabase);
    });

    const payload = {
        NhsNumber: nhsNumbers[0],
        FamilyName: inputParticipantRecord[0].family_name,
        DateOfBirth: inputParticipantRecord[0].date_of_birth
      };

    // Call the block participant function
    await test.step(`When BlockParticipant function is invoked`, async () => {
      await BlockParticipant(request, payload);
    })

    // Assert that the participant's blocked flag is set to 1 in participant management table.
    await test.step('The participant received from the api should have the blocked flag set as 1', async () => {
      const response = await getRecordsFromParticipantManagementService(request);
      expect(response.data[0].BlockedFlag).toBe(1);
    })

    // Call the Unblock participant function
    await test.step(`When UnblockParticipant function is invoked`, async () => {
      await UnblockParticipant(request, payload);
    })

    // Assert that the participant's blocked flag is set to 0 in participant management table.
    await test.step('The participant received from the api should have the blocked flag set as 0', async () => {
      const response = await getRecordsFromParticipantManagementService(request);
      expect(response.data[0].BlockedFlag).toBe(0);
    })

  });
})