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

test('@DTOSS-7584-01 @regression @e2e @epic1-high-priority Confirm NHS Number Count Integrity Across Participant Tables After Processing for ADD record', async ({ request, testData }) => {
  await test.step(`Given database does not contain 2 ADD records that will be processed`, async () => {
    await cleanupDatabaseFromAPI(request, testData.nhsNumbers);
  });

  await test.step(`When 2 ADD participants are processed via storage`, async () => {
    await processFileViaStorage(testData.runTimeParquetFile);
  });

  await test.step(`Then participants records should be 1 in both management and demographic`, async () => {
    await validateSqlDatabaseFromAPI(request, testData.checkInDatabase);
  });


});

testWithAmended('@DTOSS-7584-02 @regression @e2e @epic1-high-priority Confirm NHS Number Count Integrity Across Participant Tables After Processing for AMENDED record', async ({ request, testData }) => {
  await test.step(`Given database does not contain 2 ADD records that will be processed`, async () => {
    await cleanupDatabaseFromAPI(request, testData.nhsNumbers);
  });

  await test.step(`When 1 ADD participant is processed via storage`, async () => {
    await processFileViaStorage(testData.runTimeParquetFileAdd);
  });

  await test.step(`Then participants records should be 1 in both management and demographic`, async () => {
    await validateSqlDatabaseFromAPI(request, testData.checkInDatabaseAdd);
  });

  await test.step(`When same ADD participant record is AMENDED via storage`, async () => {
    await processFileViaStorage(testData.runTimeParquetFileAmend);
  });

  await test.step(`Then participants records should be 1 in both management and demographic and cohort manager record count should be 2`, async () => {
    await validateSqlDatabaseFromAPI(request, testData.checkInDatabaseAmend);
  });


});
