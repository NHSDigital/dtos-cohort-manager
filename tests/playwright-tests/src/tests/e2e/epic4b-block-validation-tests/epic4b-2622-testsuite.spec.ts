import { test, expect } from '@playwright/test';
import { createParquetFromJson } from '../../../parquet/parquet-multiplier';
import { getApiTestData, processFileViaStorage, cleanupDatabaseFromAPI, validateSqlDatabaseFromAPI } from '../../steps/steps';
import { getRecordsFromParticipantManagementService, BlockParticipant} from '../../../api/distributionService/bsSelectService'

test.describe('@regression @e2e @epic4b-block-tests Tests', async () => {
  //  test('@DTOSS-7610-01 AC01 Verify block a participant not processed to COHORT - ADD', async ({ request }, testInfo) => {

  //   const [checkInDatabase, inputParticipantRecord, nhsNumbers, testFilesPath] = await getApiTestData(testInfo.title);

  //   await cleanupDatabaseFromAPI(request, nhsNumbers);

  //   const parquetFile = await createParquetFromJson(nhsNumbers, inputParticipantRecord, testFilesPath);

  //   await test.step(`BlockParticipant`, async () => {

  //       const blockPayload = {
  //         NhsNumber: nhsNumbers[0],
  //         FamilyName: inputParticipantRecord[0].family_name,
  //         DateOfBirth: inputParticipantRecord[0].date_of_birth
  //       };

  //       const response = await BlockParticipant(request, blockPayload);
  //       expect(response.data[0].BlockedFlag).toBe(1);
  //   });

  //   await test.step(`When 1 ADD participant is processed via storage`, async () => {
  //     await processFileViaStorage(parquetFile);
  //   });


  //   // Assert that the participant is in the participant management table.
  //    await test.step(`Then participant should be in the participant management table`, async () => {
  //     await validateSqlDatabaseFromAPI(request, checkInDatabase);
  //   });


    // Call the block participant function

    //  // Assert that the participant's blocked flag is set to 1 in participant management table.
    //  await test.step('The participant received from the api should have the blocked flag set as 1', async () => {
    //   const response = await getRecordsFromParticipantManagementService(request);
    //   expect(response.data[0].BlockedFlag).toBe(1);
    // });

  // });

  test('@DTOSS-7614-01 AC01 Verify block a participant not processed to COHORT - Amend', async ({ request }, testInfo) => {

    const [checkInDatabase, inputParticipantRecord, nhsNumbers, testFilesPath] = await getApiTestData(testInfo.title);

    await cleanupDatabaseFromAPI(request, nhsNumbers);

    const parquetFile = await createParquetFromJson(nhsNumbers, inputParticipantRecord, testFilesPath);

     await test.step(`When ADD participant is processed via storage`, async () => {
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

    await test.step(`When Amend participant is processed via storage`, async () => {
      await processFileViaStorage(parquetFile);
    });

    await test.step(`Then exception should be in the exception management table`, async () => {
      await validateSqlDatabaseFromAPI(request, checkInDatabase);
    });
  });
});
