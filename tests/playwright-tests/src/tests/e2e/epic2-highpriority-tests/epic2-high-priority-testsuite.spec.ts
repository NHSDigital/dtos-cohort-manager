import { test, testWithAmended, expect } from '../../fixtures/test-fixtures';
import { cleanupDatabaseFromAPI, processFileViaStorage, validateSqlDatabaseFromAPI } from '../../steps/steps';


test('@DTOSS-8544-01 @regression @e2e @epic2-high-priority @Implement Validation for Eligibility Flag for Add', async ({ request, testData }) => {
  await test.step(`Given database does not contain record that will be processed`, async () => {
    await cleanupDatabaseFromAPI(request, testData.nhsNumbers);
  });

  await test.step(`When ADD participant is processed via storage`, async () => {
    await processFileViaStorage(testData.runTimeParquetFile);
  });

  await test.step(`Then NHS Numbers should be updated in the cohort`, async () => {
    await validateSqlDatabaseFromAPI(request, testData.checkInDatabase);
  });

  await test.step(`Then validate eligibility flag is always set to true for ADD`, async () => {
    await validateSqlDatabaseFromAPI(request, testData.checkInDatabase);
  });
});


testWithAmended('@DTOSS-8544-01 @regression @e2e @epic2-high-priority @DTOSS-8086 @Implement Validation for Eligibility Flag for Amended', async ({ request, testData }) => {

  await test.step(`Given database does not contain record that will be processed`, async () => {
    await cleanupDatabaseFromAPI(request, testData.nhsNumbers);
  });

  await test.step(`When ADD participant is processed via storage`, async () => {
    await processFileViaStorage(testData.runTimeParquetFileAdd);
  });

  await test.step(`Then ADD record should be updated in the cohort`, async () => {
    await validateSqlDatabaseFromAPI(request, testData.checkInDatabaseAdd);
  });


  await test.step(`When same ADD participant record is AMENDED via storage for ${testData.nhsNumberAmend}`, async () => {
    await processFileViaStorage(testData.runTimeParquetFileAmend);
  });

  await test.step(`Then validate eligibility flag is always set to true for AMENDED`, async () => {
    await validateSqlDatabaseFromAPI(request, testData.checkInDatabaseAmend);
  });
});

test('@DTOSS-8534-01 @regression @e2e @epic2-high-priority @Implement validate eligibility flag set to false for ADD', async ({ request, testData }) => {
  await test.step(`Given database does not contain record that will be processed`, async () => {
    await cleanupDatabaseFromAPI(request, testData.nhsNumbers);
  });

  await test.step(`When ADD participant is processed via storage`, async () => {
    await processFileViaStorage(testData.runTimeParquetFile);
  });

  await test.step(`Then NHS Numbers should be updated in the cohort`, async () => {
    await validateSqlDatabaseFromAPI(request, testData.checkInDatabase);
  });

  await test.step(`Then validate eligibility flag set to false for ADD`, async () => {
    await validateSqlDatabaseFromAPI(request, testData.checkInDatabase);
  });
});


testWithAmended('@DTOSS-8592-01 @regression @e2e @epic2-high-priority @Implement validate invalid flag value for Amended', async ({ request, testData }) => {

  await test.step(`Given database does not contain record that will be processed`, async () => {
    await cleanupDatabaseFromAPI(request, testData.nhsNumbers);
  });

  await test.step(`When ADD participant is processed via storage`, async () => {
    await processFileViaStorage(testData.runTimeParquetFileAdd);
  });

  await test.step(`Then validate invalid flag is set to true for ADD`, async () => {
    await validateSqlDatabaseFromAPI(request, testData.checkInDatabaseAdd);
  });


  await test.step(`When same ADD participant record is AMENDED via storage for ${testData.nhsNumberAmend}`, async () => {
    await processFileViaStorage(testData.runTimeParquetFileAmend);
  });

  await test.step(`Then validate invalid flag set to true for AMENDED`, async () => {
    await validateSqlDatabaseFromAPI(request, testData.checkInDatabaseAmend);
  });
});

test('@DTOSS-8535-01 @regression @e2e @epic2-high-priority @Implement validate invalid flag set to false for ADD', async ({ request, testData }) => {
  await test.step(`Given database does not contain record that will be processed`, async () => {
    await cleanupDatabaseFromAPI(request, testData.nhsNumbers);
  });

  await test.step(`When ADD participant is processed via storage`, async () => {
    await processFileViaStorage(testData.runTimeParquetFile);
  });

  await test.step(`Then validate invalid flag is set to false for ADD`, async () => {
    await validateSqlDatabaseFromAPI(request, testData.checkInDatabase);
  });
});


test('@DTOSS-8593-01 @regression @e2e @epic2-high-priority @Implement validate invalid flag set to true for ADD', async ({ request, testData }) => {
  await test.step(`Given database does not contain record that will be processed`, async () => {
    await cleanupDatabaseFromAPI(request, testData.nhsNumbers);
  });

  await test.step(`When ADD participant is processed via storage`, async () => {
    await processFileViaStorage(testData.runTimeParquetFile);
  });

  await test.step(`Then validate invalid flag is set to true for ADD`, async () => {
    await validateSqlDatabaseFromAPI(request, testData.checkInDatabase);
  });
});
