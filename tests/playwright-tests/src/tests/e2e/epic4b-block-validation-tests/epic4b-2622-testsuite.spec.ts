import { expect, test, testWithAmended } from '../../fixtures/test-fixtures';
import { createParquetFromJson } from '../../../parquet/parquet-multiplier';
import { getApiTestData, processFileViaStorage, validateSqlDatabaseFromAPI } from '../../steps/steps';
import { BlockParticipant} from '../../../api/distributionService/bsSelectService'
import { expectStatus } from '../../../api/responseValidators';
import { TestHooks } from '../../hooks/test-hooks';

test.describe('@regression @e2e @epic4b-block-tests Tests', async () => {
   TestHooks.setupAllTestHooks();
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

  testWithAmended('@DTOSS-7614-01 AC01 Verify block a participant not processed to COHORT - Amend', async ({ request , testData }, testInfo) => {

    // const [ inputParticipantRecord, nhsNumbers, testFilesPath] = await getApiTestData(testInfo.title);
    // const parquetFile = await createParquetFromJson(nhsNumbers, inputParticipantRecord, testFilesPath);

    await test.step(`When ADD participant is processed via storage`, async () => {
      await processFileViaStorage(testData.runTimeParquetFileAdd);
    });

    await test.step(`Then participant should be in the participant management table`, async () => {
      await validateSqlDatabaseFromAPI(request, testData.checkInDatabaseAdd);
    });

    // Call the block participant function
    await test.step(`When BlockParticipant function is invoked`, async () => {
      const blockPayload = {
        NhsNumber: testData.nhsNumbers[0],
        FamilyName: testData.inputParticipantRecord?.[0]?.family_name,
        DateOfBirth: testData.inputParticipantRecord?.[0]?.date_of_birth

        // NhsNumber: nhsNumbers[0],
        // FamilyName: inputParticipantRecord[0].family_name,
        // DateOfBirth: inputParticipantRecord[0].date_of_birth
      };

      const response = await BlockParticipant(request, blockPayload);
      expectStatus(200);
    })

  //   await test.step(`When Amend participant is processed via storage`, async () => {
  //     await processFileViaStorage(testData.runTimeParquetFileAmend);
  //   });

  //   await test.step(`Then exception should be in the exception management table`, async () => {
  //     await validateSqlDatabaseFromAPI(request, testData.checkInDatabaseAmend);
  //   });
   });

// test('@DTOSS-7614-01 AC01 Verify block a participant not processed to COHORT - Delete', async ({ request }, testInfo) => {

//     const [checkInDatabase, inputParticipantRecord, nhsNumbers, testFilesPath] = await getApiTestData(testInfo.title);

//     const parquetFile = await createParquetFromJson(nhsNumbers, inputParticipantRecord, testFilesPath);

//     await test.step(`When ADD participant is processed via storage`, async () => {
//       await processFileViaStorage(parquetFile);
//     });

//     await test.step(`Then participant should be in the participant management table`, async () => {
//       await validateSqlDatabaseFromAPI(request, checkInDatabase);
//     });

//     // Call the block participant function
//     await test.step(`When BlockParticipant function is invoked`, async () => {
//       const blockPayload = {
//         NhsNumber: nhsNumbers[0],
//         FamilyName: inputParticipantRecord[0].family_name,
//         DateOfBirth: inputParticipantRecord[0].date_of_birth
//       };

//       const response = await BlockParticipant(request, blockPayload);
    //   expect(response.data[0].BlockedFlag).toBe(1);
    // })

    // await test.step(`When Delete participant is processed via storage`, async () => {
    //   await processFileViaStorage(parquetFile);
    // });

    // await test.step(`Then exception should be in the exception management table`, async () => {
    //   await validateSqlDatabaseFromAPI(request, checkInDatabase);
    // });
  //});
});


