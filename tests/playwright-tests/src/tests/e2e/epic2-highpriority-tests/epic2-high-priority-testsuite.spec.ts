import { test } from '@playwright/test';
import { cleanupDatabaseFromAPI, getApiTestData, getTestData, processFileViaStorage, validateSqlDatabaseFromAPI } from '../../steps/steps';


test.only('06 @epic2-high-priority @regression @DTOSS-8544 @Implement Validation for Eligibility Flag for Add', async ({ request }, testInfo) => {

  const [checkInDatabase, nhsNumber, parquetFileAdd] = await getTestData(testInfo.title);


  await test.step(`Given database does not contain record that will be processed`, async () => {
    await cleanupDatabaseFromAPI(request, nhsNumber);
  });

  await test.step(`When ADD participant is processed via storage`, async () => {
    await processFileViaStorage(parquetFileAdd!);
  });

  await test.step(`Then NHS Numbers should be should be updated in the cohort`, async () => {
    await validateSqlDatabaseFromAPI(request, checkInDatabase);
  });

  await test.step(`Then validate eligibility flag is always set to true for ADD`, async () => {
    await validateSqlDatabaseFromAPI(request, checkInDatabase);
  });

});
