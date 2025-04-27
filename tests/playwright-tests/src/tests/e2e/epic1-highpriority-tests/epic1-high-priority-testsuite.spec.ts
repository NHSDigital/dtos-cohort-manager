import { test, testWithAmended, expect } from '../../fixtures/test-fixtures';
import { cleanupDatabaseFromAPI, processFileViaStorage, validateSqlDatabaseFromAPI } from '../../steps/steps';


test.only('@DTOSS-8519-01 @regression @e2e Verify file upload into participants table for ADD', async ({ request, testData }) => {
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
