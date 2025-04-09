import { test } from '@playwright/test';
import { cleanupDatabase, validateSqlData } from '../../database/sqlVerifier';
import { getTestData, processFileViaStorage, validateSqlDatabase } from '../steps/steps'



test.describe('Smoke Tests', () => {
  test('01 @smoke @DTOSS-6256 Verify file upload and cohort distribution process for ADD', async ({ request }, testInfo) => {
    console.info(`Running test: ${testInfo.title}`);

    const [checkInDatabase, nhsNumbers, parquetFile] = await getTestData(testInfo.title);

    await test.step(`Given database does not contain 2 ADD records that will be processed`, async () => {
      await cleanupDatabase(nhsNumbers);
    });

    await test.step(`When 2 ADD participants are processed via storage`, async () => {
      await processFileViaStorage(parquetFile!);
    });

    await test.step(`Then NHS Numbers should be should be updated in the cohort`, async () => {
      await validateSqlDatabase(checkInDatabase);
    });

  });

  test('02 @smoke @DTOSS-6257 Verify file upload and cohort distribution process for ADD followed by AMENDED records', async ({ request }, testInfo) => {
    console.info(`Running test: ${testInfo.title}`);

    const [checkInDatabase, nhsNumber, parquetFileAdd] = await getTestData(testInfo.title);
    const [checkInDatabaseAmend, nhsNumberAmend, parquetFileAmend] = await getTestData(testInfo.title, "AMENDED");


    await test.step(`Given database does not contain record that will be processed`, async () => {
      await cleanupDatabase(nhsNumber);
    });

    await test.step(`When ADD participant is processed via storage`, async () => {
      await processFileViaStorage(parquetFileAdd!);
    });

    await test.step(`Then ADD record should be updated in the cohort`, async () => {
      await validateSqlData(checkInDatabase);
    });

    await test.step(`When same ADD participant record is AMENDED via storage: ${nhsNumberAmend}`, async () => {
      await processFileViaStorage(parquetFileAmend!);
    });

    await test.step(`Then AMENDED record name should be updated in the cohort`, async () => {
      await validateSqlDatabase(checkInDatabaseAmend);
    });

  });

  test('03 @smoke @DTOSS-6406 Verify file upload handles invalid GP Practice Code Exception', async ({ request }, testInfo) => {

    const [checkInDatabase, nhsNumber, parquetFileAdd] = await getTestData(testInfo.title);

    await test.step(`Given database does not contain record that will be processed`, async () => {
      await cleanupDatabase(nhsNumber);
    });

    await test.step(`When ADD participant is processed via storage`, async () => {
      await processFileViaStorage(parquetFileAdd!);
    });

    await test.step(`Then the Exception table should contain the below details for the NHS Number`, async () => {
      await validateSqlDatabase(checkInDatabase);
    });
  });

  test('04 @smoke @DTOSS-6407 Verify file upload handles EmptyDOB Exception', async ({ request }, testInfo) => {

    const [checkInDatabase, nhsNumber, parquetFileAdd] = await getTestData(testInfo.title);
    const [checkInDatabaseAmend, nhsNumberAmend, parquetFileAmend] = await getTestData(testInfo.title, "AMENDED");

    await test.step(`Given database does not contain record that will be processed`, async () => {
      await cleanupDatabase(nhsNumber);
    });

    await test.step(`When ADD participant is processed via storage`, async () => {
      await processFileViaStorage(parquetFileAdd!);
    });

    await test.step(`Then ADD record should be updated in the cohort`, async () => {
      await validateSqlData(checkInDatabase);
    });

    await test.step(`When same ADD participant record is AMENDED via storage for ${nhsNumberAmend}`, async () => {
      await processFileViaStorage(parquetFileAmend!);
    });

    await test.step(`Then the Exception table should contain the below details for the NHS Number`, async () => {
      await validateSqlDatabase(checkInDatabaseAmend);
    });

  });

  test('05 @smoke @DTOSS-7960 Verify GP Practice Code Exception flag in participant management set to 1', async ({ request }, testInfo) => {
    console.info(`Running test: ${testInfo.title}`);

    const [checkInDatabase, nhsNumbers, parquetFile] = await getTestData(testInfo.title);

    await test.step(`Given database does not contain records that will be processed: ${nhsNumbers}  `, async () => {
      await cleanupDatabase(nhsNumbers);
    });

    await test.step(`When participants are processed via storage`, async () => {
      await processFileViaStorage(parquetFile!);
    });

    await test.step(`Then records should be updated in the cohort`, async () => {
      await validateSqlDatabase(checkInDatabase);
    });
  });

});


