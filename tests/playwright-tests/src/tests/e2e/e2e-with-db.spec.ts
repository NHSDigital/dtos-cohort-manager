import { test } from '@playwright/test';
import { cleanupDatabase } from '../../database/sqlVerifier'; // TODO Migrate to RemoveParticipant API
import { getTestData, processFileViaStorage, validateSqlDatabase, validateSqlDatabaseFromAPI } from '../steps/steps'



test.describe('Smoke Tests', () => {

  test('01 @smoke @DTOSS-6256 @api Verify file upload and cohort distribution process for ADD', async ({ request }, testInfo) => {
    const [checkInDatabase, nhsNumbers, parquetFile] = await getTestData(testInfo.title);

    await test.step(`Given database does not contain 2 ADD records that will be processed`, async () => {
      await cleanupDatabase(nhsNumbers);
    });

    await test.step(`When 2 ADD participants are processed via storage`, async () => {
      await processFileViaStorage(parquetFile!);
    });

    await test.step(`Then NHS Numbers should be should be updated in the cohort`, async () => {
      await validateSqlDatabaseFromAPI(request, checkInDatabase);
    });

  });

  test('02 @smoke @DTOSS-6257 @db Verify file upload and cohort distribution process for ADD followed by AMENDED records', async ({request }, testInfo) => {

    const [checkInDatabase, nhsNumber, parquetFileAdd] = await getTestData(testInfo.title);
    const [checkInDatabaseAmend, nhsNumberAmend, parquetFileAmend] = await getTestData(testInfo.title, "AMENDED");

    await test.step(`Given database does not contain record that will be processed`, async () => {
      await cleanupDatabase(nhsNumber);
    });

    await test.step(`When ADD participant is processed via storage`, async () => {
      await processFileViaStorage(parquetFileAdd!);
    });

    await test.step(`Then ADD record should be updated in the cohort`, async () => {
      await validateSqlDatabase(checkInDatabase);
    });

    await test.step(`When same ADD participant record is AMENDED via storage`, async () => {
      await processFileViaStorage(parquetFileAmend!);
    });

    await test.step(`Then AMENDED record name should be updated in the cohort: ${nhsNumberAmend}`, async () => {
      await validateSqlDatabaseFromAPI(request, checkInDatabaseAmend);
    });

  });
  //TODO: FIX validation logic for Exception flag = `1` via API
  test('05 @smoke @DTOSS-7960 @api Verify GP Practice Code Exception flag in participant management set to 1', async ({ request }, testInfo) => {
    console.info(`Running test: ${testInfo.title}`); //TODO move to beforeEach

    const [checkInDatabase, nhsNumbers, parquetFile] = await getTestData(testInfo.title);

    await test.step(`Given database does not contain records that will be processed: ${nhsNumbers}  `, async () => {
      await cleanupDatabase(nhsNumbers);
    });

    await test.step(`When participants are processed via storage`, async () => {
      await processFileViaStorage(parquetFile!);
    });

    await test.step(`Then records should be updated in the cohort`, async () => {
      await validateSqlDatabaseFromAPI(request, checkInDatabase);
    });
  });

});


