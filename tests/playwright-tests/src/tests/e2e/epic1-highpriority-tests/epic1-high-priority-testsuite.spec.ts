import { test, testWithAmended } from '../../fixtures/test-fixtures';
import { cleanupDatabaseFromAPI, processFileViaStorage, validateSqlDatabaseFromAPI } from '../../steps/steps';




test.describe('@regression @e2e @epic1-high-priority participant ADD process', () => {

  test.beforeEach(async ({ request, testData }) => {
    await test.step(`Given database does not contain ADD records that will be processed`, async () => {
      await cleanupDatabaseFromAPI(request, testData.nhsNumbers);
    });

    await test.step(`When ADD participants are processed via storage`, async () => {
      await processFileViaStorage(testData.runTimeParquetFile);
    });

  });

  test('@DTOSS-3648-01 Verify file upload into participants table for ADD', async ({ request, testData }) => {

    await test.step(`Then participant NHS number should be should be updated in both management and demographic`, async () => {
      await validateSqlDatabaseFromAPI(request, testData.checkInDatabase);
    });

  });

  test('@DTOSS-3661-01 Confirm NHS Number Count Integrity Across Participant Tables After Processing for ADD record', async ({ request, testData }) => {

    await test.step(`Then participants records should be 1 in both management and demographic`, async () => {
      await validateSqlDatabaseFromAPI(request, testData.checkInDatabase);
    });

  });

  test('@DTOSS-3662-01 Verify ADD records that trigger a non-fatal validation rule reach internal participant tables but not Cohort distribution', async ({ request, testData }) => {

    await test.step(`Then participants records should be 1 in both management and demographic tables, the cohort table should have 0 records, and the exception table should have 1 record with an invalid primary care provider GP practice code and rule ID 36`, async () => {
      await validateSqlDatabaseFromAPI(request, testData.checkInDatabase);
    });

  });

  test('@DTOSS-3197-01 Verify ADD records is processed without any Exception', async ({ request, testData }) => {

    await test.step(`Then NHS numbers should be updated in the participants table, 1 record in the cohort table, and 0 records in the exception table.`, async () => {
      await validateSqlDatabaseFromAPI(request, testData.checkInDatabase);
    });

  });

  test('@DTOSS-3744-01 Verify a file is uploaded to storage successfully', async ({ request, testData }) => {

    await test.step(`Then NHS numbers should be updated in the participants table, and the content should match.`, async () => {
      await validateSqlDatabaseFromAPI(request, testData.checkInDatabase);
    });

  });

  test('@DTOSS-3660-01 Verify Successful Data Processing and Storage in Cohort Manager for Add', async ({ request, testData }) => {

    await test.step(`"Then NHS numbers should be updated in the participants table, 1 record in the cohort table after data processing, and 0 records in the exception table.`, async () => {
      await validateSqlDatabaseFromAPI(request, testData.checkInDatabase);
    });

  });

});

test.describe('@regression @e2e @epic1-high-priority participant AMENDED process', () => {

  testWithAmended.beforeEach(async ({ request, testData }) => {

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
  });

  testWithAmended('@DTOSS-3217-01 Verify AMENDED records reach the participant tables', async ({ request, testData }) => {


    await test.step(`Then AMENDED record name should be updated in the participants demographic table`, async () => {
      await validateSqlDatabaseFromAPI(request, testData.checkInDatabaseAmend);
    });
  });

  testWithAmended('@DTOSS-3661-02 Confirm NHS Number Count Integrity Across Participant Tables After Processing for AMENDED record', async ({ request, testData }) => {

    await test.step(`Then participants records should be 1 in both management and demographic and cohort manager record count should be 2`, async () => {
      await validateSqlDatabaseFromAPI(request, testData.checkInDatabaseAmend);
    });


  });


  testWithAmended('@DTOSS-3662-02 Verify AMENDED records with non-fatal validation issues reach participant tables with partial Cohort distribution entries', async ({ request, testData }) => {

    await test.step(`Then participants records should be 1 in both management and demographic tables, the cohort manager record count should be 1, and the exception table should have 1 record with an invalid date of birth and rule ID 17`, async () => {
      await validateSqlDatabaseFromAPI(request, testData.checkInDatabaseAmend);
    });


  });

  testWithAmended('@DTOSS-3217-02 Verify AMENDED records is processed without any Exception', async ({ request, testData }) => {

    await test.step(`Then participants records should be 1 in both management and demographic tables, the cohort manager record count should be 2, and the exception table should have 0 records`, async () => {
      await validateSqlDatabaseFromAPI(request, testData.checkInDatabaseAmend);
    });

  });

});

test.describe('@regression @e2e @epic1-high-priority invalid file for ADD process', () => {
  test.only('@DTOSS-3192-01 Verify that a file with an invalid name creates a validation exception', async ({ request, testData }) => {
    await test.step(`Given database does not contain 1 ADD record that will be processed`, async () => {
      await cleanupDatabaseFromAPI(request, testData.nhsNumbers);
    });

    await processFileViaStorage(testData.runTimeParquetFileInvalid);


    await test.step(`Then exception should have 1 record with The file failed file validation. Check the file Exceptions blob store. and rule id 0`, async () => {
      await validateSqlDatabaseFromAPI(request, testData.checkInDatabase);
    });


  });
});










