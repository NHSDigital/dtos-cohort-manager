import { test } from '@playwright/test';
import { cleanupDatabaseFromAPI, getApiTestData, getTestData, processFileViaStorage, validateSqlDatabaseFromAPI } from '../../steps/steps';


test('06 @epic2-high-priority @DTOSS-8544 @Implement Validation for Eligibility Flag for Add', async ({ request }, testInfo) => {
  const [checkInDatabase, nhsNumber, parquetFileAdd] = await getTestData(testInfo.title);

  await test.step(`Given database does not contain record that will be processed`, async () => {
    await cleanupDatabaseFromAPI(request, nhsNumber);
  });

  await test.step(`When ADD participant is processed via storage`, async () => {
    await processFileViaStorage(parquetFileAdd!);
  });

  await test.step(`Then NHS Numbers should be updated in the cohort`, async () => {
    await validateSqlDatabaseFromAPI(request, checkInDatabase);
  });

  await test.step(`Then validate eligibility flag is always set to true for ADD`, async () => {
    await validateSqlDatabaseFromAPI(request, checkInDatabase);
  });
});



test('06 @epic2-high-priority @DTOSS-8086 @Implement Validation for Eligibility Flag for Amended', async ({ request }, testInfo) => {
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

  await test.step(`Then validate eligibility flag is always set to true for AMENDED`, async () => {
    await validateSqlDatabaseFromAPI(request, checkInDatabase);
  });
});

test('07 @epic2-high-priority @DTOSS-8534 @Implement validate eligibility flag set to null for ADD should raise exception', async ({ request }, testInfo) => {
  const [checkInDatabase, nhsNumber, parquetFileAdd] = await getTestData(testInfo.title);

  await test.step(`Given database does not contain record that will be processed`, async () => {
    await cleanupDatabaseFromAPI(request, nhsNumber);
  });

  await test.step(`When ADD participant is processed via storage`, async () => {
    await processFileViaStorage(parquetFileAdd!);
  });

  await test.step(`Then NHS Numbers should be updated in the cohort`, async () => {
    await validateSqlDatabaseFromAPI(request, checkInDatabase);
  });

  await test.step(`Then validate eligibility flag set to flase for ADD`, async () => {
    await validateSqlDatabaseFromAPI(request, checkInDatabase);
  });
});
