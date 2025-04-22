import { test } from '@playwright/test';
import { cleanupDatabaseFromAPI, getTestData, processFileViaStorage, validateSqlDatabaseFromAPI } from '../steps/steps'



test.describe.parallel('Positive @smoke Tests', () => {

  test('01 @smoke @e2e @DTOSS-6256 @api Verify file upload and cohort distribution process for ADD', async ({ request }, testInfo) => {
    const [checkInDatabase, nhsNumbers, parquetFile] = await getTestData(testInfo.title);

    await test.step(`Given database does not contain 2 ADD records that will be processed`, async () => {
      await cleanupDatabaseFromAPI(request, nhsNumbers);
    });

    await test.step(`When 2 ADD participants are processed via storage`, async () => {
      await processFileViaStorage(parquetFile!);
    });

    await test.step(`Then NHS Numbers should be should be updated in the cohort`, async () => {
      await validateSqlDatabaseFromAPI(request, checkInDatabase);
    });

  });

  test('02 @smoke @e2e @DTOSS-6257 @db Verify file upload and cohort distribution process for ADD followed by AMENDED records', async ({ request }, testInfo) => {

    const [checkInDatabase, nhsNumber, parquetFileAdd] = await getTestData(testInfo.title);
    const [checkInDatabaseAmend, nhsNumberAmend, parquetFileAmend] = await getTestData(testInfo.title, "AMENDED");

    await test.step(`Given database does not contain record that will be processed`, async () => {
      await cleanupDatabaseFromAPI(request, nhsNumber);
    });

    await test.step(`When ADD participant is processed via storage`, async () => {
      await processFileViaStorage(parquetFileAdd!);
    });

    await test.step(`Then ADD record should be updated in the cohort`, async () => {
      await validateSqlDatabaseFromAPI(request, checkInDatabase);
    });

    await test.step(`When same ADD participant record is AMENDED via storage`, async () => {
      await processFileViaStorage(parquetFileAmend!);
    });

    await test.step(`Then AMENDED record name should be updated in the cohort: ${nhsNumberAmend}`, async () => {
      await validateSqlDatabaseFromAPI(request, checkInDatabaseAmend);
    });

  });

  test('04 @smoke @e2e @DTOSS-6407 Verify file upload handles EmptyDOB Exception', async ({ request }, testInfo) => {

    const [checkInDatabase, nhsNumber, parquetFileAdd] = await getTestData(testInfo.title);
    const [checkInDatabaseAmend, nhsNumberAmend, parquetFileAmend] = await getTestData(testInfo.title, "AMENDED");

    await test.step(`Given database does not contain record that will be processed`, async () => {
      await cleanupDatabaseFromAPI(request, nhsNumber);
    });

    await test.step(`When ADD participant is processed via storage`, async () => {
      await processFileViaStorage(parquetFileAdd!);
    });

    await test.step(`Then ADD record should be updated in the cohort`, async () => {
      await validateSqlDatabaseFromAPI(request, checkInDatabase);
    });

    await test.step(`When same ADD participant record is AMENDED via storage for ${nhsNumberAmend}`, async () => {
      await processFileViaStorage(parquetFileAmend!);
    });

    await test.step(`Then the Exception table should contain the below details for the NHS Number`, async () => {
      await validateSqlDatabaseFromAPI(request, checkInDatabaseAmend);
    });

  });

});


test.describe.parallel('Exception @smoke Tests', () => {

  test('03 @smoke @e2e @DTOSS-6406 Verify file upload handles invalid GP Practice Code Exception', async ({ request }, testInfo) => {

    const [checkInDatabase, nhsNumber, parquetFileAdd] = await getTestData(testInfo.title);

    await test.step(`Given database does not contain record that will be processed`, async () => {
      await cleanupDatabaseFromAPI(request, nhsNumber);
    });

    await test.step(`When ADD participant is processed via storage`, async () => {
      await processFileViaStorage(parquetFileAdd!);
    });

    await test.step(`Then the Exception table should contain the below details for the NHS Number`, async () => {
      await validateSqlDatabaseFromAPI(request, checkInDatabase);
    });
  });

  test('05 @smoke @e2e @DTOSS-7960 @api Verify GP Practice Code Exception flag in participant management set to 1', async ({ request }, testInfo) => {
    console.info(`Running test: ${testInfo.title}`);

    const [checkInDatabase, nhsNumbers, parquetFile] = await getTestData(testInfo.title);

    await test.step(`Given database does not contain records that will be processed: ${nhsNumbers}  `, async () => {
      await cleanupDatabaseFromAPI(request, nhsNumbers);
    });

    await test.step(`When participants are processed via storage`, async () => {
      await processFileViaStorage(parquetFile!);
    });

    await test.step(`Then records should be updated in the cohort`, async () => {
      await validateSqlDatabaseFromAPI(request, checkInDatabase);
    });
  });

});



