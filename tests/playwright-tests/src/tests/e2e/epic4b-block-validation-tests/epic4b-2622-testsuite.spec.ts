import { test, expect } from '@playwright/test';
import { createParquetFromJson } from '../../../parquet/parquet-multiplier';
import { getApiTestData, processFileViaStorage, cleanupDatabaseFromAPI, validateSqlDatabaseFromAPI } from '../../steps/steps';
import { BlockParticipant} from '../../../api/distributionService/bsSelectService'
import { TestHooks } from '../../hooks/test-hooks';
import { getRecordsFromParticipantManagementService } from '../../../api/distributionService/participantService';


test.describe.serial(' @api Positive - Block Participant called', async () => {

  TestHooks.setupAllTestHooks();

  test('@DTOSS-7610-01 @smoke @api - @TC1 Verify block a participant', async ({ request }, testInfo) => {

    const [checkInDatabase, inputParticipantRecord, nhsNumbers, testFilesPath] = await getApiTestData(testInfo.title);

    await cleanupDatabaseFromAPI(request, nhsNumbers);

    const parquetFile = await createParquetFromJson(nhsNumbers, inputParticipantRecord, testFilesPath);

    await test.step(`When I ADD participant is processed via storage`, async () => {
      await processFileViaStorage(parquetFile);
    });

    await test.step(`Then participant should be in the participant management table`, async () => {
      await validateSqlDatabaseFromAPI(request, checkInDatabase);
    });

    // Call the block participant function
    await test.step(`When BlockParticipant function is invoked`, async () => {
      const blockPayload = {
        NhsNumber: nhsNumbers[0],
        FamilyName: inputParticipantRecord[0].family_name,
        DateOfBirth: inputParticipantRecord[0].date_of_birth
      };

      const response = await BlockParticipant(request, blockPayload);
    })

    // Assert that the participant's blocked flag is set to 1 in participant management table.
    await test.step('The participant received from the api should have the blocked flag set as 1', async () => {
      const response = await getRecordsFromParticipantManagementService(request);
      expect(response.data[0].BlockedFlag).toBe(1);
    })

  });
})
