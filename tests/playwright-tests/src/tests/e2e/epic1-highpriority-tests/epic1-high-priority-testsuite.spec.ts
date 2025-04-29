import { test, testWithAmended, expect } from '../../fixtures/test-fixtures';
import { cleanupDatabaseFromAPI, processFileViaStorage, validateSqlDatabaseFromAPI } from '../../steps/steps';


test('@DTOSS-8519-01 @regression @e2e @epic1-high-priority Verify file upload into participants table for ADD', async ({ request, testData }) => {
  await test.step(`Given database does not contain 2 ADD records that will be processed`, async () => {
    await cleanupDatabaseFromAPI(request, testData.nhsNumbers);
  });

  await test.step(`When 2 ADD participants are processed via storage`, async () => {
    await processFileViaStorage(testData.runTimeParquetFile);
  });

  await test.step(`Then NHS Numbers should be should be updated in the participants table`, async () => {
    await validateSqlDatabaseFromAPI(request, testData.checkInDatabase);
  });
});

testWithAmended('@DTOSS-8521-01 @regression @e2e @epic1-high-priority @Verify AMENDED records reach the participant tables', async ({ request, testData }) => {

  await test.step(`Given database does not contain record that will be processed`, async () => {
    await cleanupDatabaseFromAPI(request, testData.nhsNumbers);
  });

  await test.step(`When ADD participants is processed via storage`, async () => {
    await processFileViaStorage(testData.runTimeParquetFileAdd);
  });

  await test.step(`Then ADD record should be updated in the participants management table`, async () => {
    await validateSqlDatabaseFromAPI(request, testData.checkInDatabaseAdd);
  });

  await test.step(`When same ADD participant record is AMENDED via storage`, async () => {
    await processFileViaStorage(testData.runTimeParquetFileAmend);
  });

  await test.step(`Then AMENDED record name should be updated in the participants demographic table`, async () => {
    await validateSqlDatabaseFromAPI(request, testData.checkInDatabaseAmend);
  });
});



// test('08 @smoke @DTOSS-8519 @api Verify file upload into participants table for ADD', async ({ request }, testInfo) => {
//   const [checkInDatabase, nhsNumbers, parquetFile] = await getTestData(testInfo.title);

//   await test.step(`Given database does not contain 2 ADD records that will be processed`, async () => {
//     await cleanupDatabaseFromAPI(request, nhsNumbers);
//   });

//   await test.step(`When 2 ADD participants are processed via storage`, async () => {
//     await processFileViaStorage(parquetFile!);
//   });

//   await test.step(`Then NHS Numbers should be should be updated in the participants table`, async () => {
//     await validateSqlDatabaseFromAPI(request, checkInDatabase);
//   });

// })

// test('09 @smoke @DTOSS-8520 @ut @api Verify AMENDED records reach the participant tables', async ({ request }, testInfo) => {

//   const [checkInDatabase, nhsNumber, parquetFileAdd] = await getTestData(testInfo.title);
//   const [checkInDatabaseAmend, nhsNumberAmend, parquetFileAmend] = await getTestData(testInfo.title, "AMENDED");

//   await test.step(`Given database does not contain record that will be processed`, async () => {
//     await cleanupDatabaseFromAPI(request, nhsNumber);
//   });

//   await test.step(`When ADD participant is processed via storage`, async () => {
//     await processFileViaStorage(parquetFileAdd!);
//   });

//   await test.step(`Then ADD record should be updated in the participants`, async () => {
//     await validateSqlDatabaseFromAPI(request, checkInDatabase);
//   });

//   await test.step(`When same ADD participant record is AMENDED via storage`, async () => {
//     await processFileViaStorage(parquetFileAmend!);
//   });

//   await test.step(`Then AMENDED record name should be updated in the participants table: ${nhsNumberAmend}`, async () => {
//     await validateSqlDatabaseFromAPI(request, checkInDatabaseAmend);
//   });

// })

// test('10 @smoke @DTOSS-8521 @api Confirm NHS Number Count Integrity Across Participant Tables After Processing for ADD record', async ({ request }, testInfo) => {

//   const [checkInDatabase, nhsNumbers, parquetFile] = await getTestData(testInfo.title);
//   const expectedCount = 1;

//   await test.step(`Given database does not contain 2 ADD records that will be processed`, async () => {
//     await cleanupDatabaseFromAPI(request, nhsNumbers);
//   });

//   await test.step(`When participants are processed via storage`, async () => {
//     await processFileViaStorage(parquetFile!);
//   });

//   await test.step(`Then NHS Numbers should be should be updated in the participants table`, async () => {
//     await validateSqlDatabaseFromAPI(request, checkInDatabase );
//   });

//   await test.step(`Then NHS Numbers should be should be updated in the participants table`, async () => {
//     await validateSqlDatabaseFromAPIcount(request, checkInDatabase , expectedCount);
//   });

//   })


