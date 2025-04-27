import { test, testWithAmended, expect } from '../../fixtures/test-fixtures';
import { cleanupDatabaseFromAPI, processFileViaStorage, validateSqlDatabaseFromAPI } from '../../steps/steps';

test('@DTOSS-6256-01 @smoke @e2e @ds Verify file upload and cohort distribution process for ADD', async ({ request, testData }) => {
  await test.step(`Given database does not contain records that will be processed`, async () => {
    await cleanupDatabaseFromAPI(request, testData.nhsNumbers);
  });

  await test.step(`When participants are processed via storage`, async () => {
    await processFileViaStorage(testData.runTimeParquetFile);
  });

  await test.step(`Then NHS Numbers should be updated in the cohort`, async () => {
    await validateSqlDatabaseFromAPI(request, testData.checkInDatabase);
  });
});

testWithAmended('@DTOSS-6257-01 @smoke @e2e @ds Verify file upload and cohort distribution process for ADD followed by AMENDED records', async ({ request, testData }) => {
  await test.step(`Given database does not contain record that will be processed`, async () => {
    await cleanupDatabaseFromAPI(request, testData.nhsNumbers);
  });

  await test.step(`When ADD participant is processed via storage`, async () => {
    await processFileViaStorage(testData.runTimeParquetFileAdd);
  });

  await test.step(`Then ADD record should be updated in the cohort`, async () => {
    await validateSqlDatabaseFromAPI(request, testData.checkInDatabaseAdd);
  });

  await test.step(`When same ADD participant record is AMENDED via storage`, async () => {
    await processFileViaStorage(testData.runTimeParquetFileAmend);
  });

  await test.step(`Then AMENDED record should be updated in the cohort`, async () => {
    await validateSqlDatabaseFromAPI(request, testData.checkInDatabaseAmend);
  });
});

testWithAmended('@DTOSS-6407-01 @smoke @e2e @ds Verify file upload handles EmptyDOB Exception', async ({ request, testData }) => {
  await test.step(`Given database does not contain record that will be processed`, async () => {
    await cleanupDatabaseFromAPI(request, testData.nhsNumbers);
  });

  await test.step(`When ADD participant is processed via storage`, async () => {
    await processFileViaStorage(testData.runTimeParquetFileAdd);
  });

  await test.step(`Then ADD record should be updated in the cohort`, async () => {
    await validateSqlDatabaseFromAPI(request, testData.checkInDatabaseAdd);
  });

  await test.step(`When same ADD participant record is AMENDED via storage`, async () => {
    await processFileViaStorage(testData.runTimeParquetFileAmend);
  });

  await test.step(`Then the Exception table should contain the details for the NHS Number`, async () => {
    await validateSqlDatabaseFromAPI(request, testData.checkInDatabaseAmend);
  });
});

test.describe.parallel('Exception Tests', () => {
test.only('@DTOSS-6406-01 @smoke @e2e @ds Verify file upload handles invalid GP Practice Code Exception', async ({ request, testData }) => {
  await test.step(`Given database does not contain record that will be processed`, async () => {
    await cleanupDatabaseFromAPI(request, testData.nhsNumbers);
  });

  await test.step(`When ADD participant is processed via storage`, async () => {
    await processFileViaStorage(testData.runTimeParquetFile);
  });

  await test.step(`Then the Exception table should contain the details for the NHS Number`, async () => {
    await validateSqlDatabaseFromAPI(request, testData.checkInDatabase);
  });
});

test('@DTOSS-7960-01 @smoke @e2e @ds Verify GP Practice Code Exception flag in participant management set to 1', async ({ request, testData }) => {
  await test.step(`Given database does not contain records that will be processed`, async () => {
    await cleanupDatabaseFromAPI(request, testData.nhsNumbers);
  });

  await test.step(`When participants are processed via storage`, async () => {
    await processFileViaStorage(testData.runTimeParquetFile);
  });

  await test.step(`Then records should be updated in the cohort`, async () => {
    await validateSqlDatabaseFromAPI(request, testData.checkInDatabase);
  });
});
});
