import path from 'path';
import { test, testWithAmended, expect } from '../../fixtures/test-fixtures';
import fs from 'fs';
import { cleanupDatabaseFromAPI, processFileViaStorage, validateSqlDatabaseFromAPI } from '../../steps/steps';


test('@DTOSS-8519-01 @regression @e2e @epic1-high-priority  @kt Verify file upload into participants table for ADD', async ({ request, testData }) => {
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

testWithAmended('@DTOSS-8521-01 @regression @e2e @epic1-high-priority  @kt @Verify AMENDED records reach the participant tables', async ({ request, testData }) => {

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

test('@DTOSS-7584-01 @regression @e2e @epic1-high-priority  @kt Confirm NHS Number Count Integrity Across Participant Tables After Processing for ADD record', async ({ request, testData }) => {
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

testWithAmended('@DTOSS-7584-02 @regression @e2e @epic1-high-priority  @kt Confirm NHS Number Count Integrity Across Participant Tables After Processing for AMENDED record', async ({ request, testData }) => {
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

test('@DTOSS-7585-01 @regression @e2e @epic1-high-priority  @kt Verify ADD records that trigger a non-fatal validation rule reach internal participant tables but not Cohort distribution', async ({ request, testData }) => {
  await test.step(`Given database does not contain 2 ADD records that will be processed`, async () => {
    await cleanupDatabaseFromAPI(request, testData.nhsNumbers);
  });

  await test.step(`When 2 ADD participants are processed via storage`, async () => {
    await processFileViaStorage(testData.runTimeParquetFile);
  });

  await test.step(`Then participants records should be 1 in both management and demographic
                   AND  cohort should have 0 records
                   AND  exception should have 1 record with Invalid primary care provider GP practice code and rule id 36`, async () => {
    await validateSqlDatabaseFromAPI(request, testData.checkInDatabase);
  });


});

testWithAmended('@DTOSS-7585-02 @regression @e2e @epic1-high-priority  @kt Verify AMENDED records with non-fatal validation issues reach participant tables with partial Cohort distribution entries', async ({ request, testData }) => {
  await test.step(`Given database does not contain 1 ADD record that will be processed`, async () => {
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

  await test.step(`Then participants records should be 1 in both management and demographic
                  AND cohort manager record count should be 1
                  AND exception should have 1 record with Date of birth invalid and rule id 17`, async () => {
    await validateSqlDatabaseFromAPI(request, testData.checkInDatabaseAmend);
  });


});

testWithAmended('@DTOSS-7589-01 @regression @e2e @epic1-high-priority  @kt Verify AMENDED records is processed without any Exception', async ({ request, testData }) => {
  await test.step(`Given database does not contain 1 ADD record that will be processed`, async () => {
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

  await test.step(`Then participants records should be 1 in both management and demographic
                  AND cohort manager record count should be 2
                  AND exception should have 0 record`, async () => {
    await validateSqlDatabaseFromAPI(request, testData.checkInDatabaseAmend);
  });


});

test('@DTOSS-7590-01 @regression @e2e @epic1-high-priority  @kt Verify ADD records is processed without any Exception', async ({ request, testData }) => {
  await test.step(`Given database does not contain 2 ADD records that will be processed`, async () => {
    await cleanupDatabaseFromAPI(request, testData.nhsNumbers);
  });

  await test.step(`When 2 ADD participants are processed via storage`, async () => {
    await processFileViaStorage(testData.runTimeParquetFile);
  });

  await test.step(`Then NHS Numbers should be should be updated in the participants table
                   AND 1 record in cohort table
                   AND no records in exception table`, async () => {
    await validateSqlDatabaseFromAPI(request, testData.checkInDatabase);
  });
});

test('@DTOSS-7587-01 @regression @e2e @epic1-high-priority  @kt Verify a file is uploaded to storage successfully', async ({ request, testData }) => {
  await test.step(`Given database does not contain 2 ADD records that will be processed`, async () => {
    await cleanupDatabaseFromAPI(request, testData.nhsNumbers);
  });

  await test.step(`When 2 ADD participants are processed via storage`, async () => {
    await processFileViaStorage(testData.runTimeParquetFile);
  });

  await test.step(`Then NHS Numbers should be should be updated in the participants table
                   AND content should match`, async () => {
    await validateSqlDatabaseFromAPI(request, testData.checkInDatabase);
  });
});

test('@DTOSS-7588-01 @regression @e2e @epic1-high-priority @kt Verify that a file with an invalid name creates a validation exception', async ({ request, testData }) => {
  await test.step(`Given database does not contain 1 ADD record that will be processed`, async () => {
    await cleanupDatabaseFromAPI(request, testData.nhsNumbers);
  });

  await test.step(`When 1 ADD participant is processed via storage`, async () => {
    const tempFileName = "Exception_1B8F53_-_CAAS_BREAST_screening_@.parquet";
 const tempDir = path.join(__dirname, 'temp');
    if (!fs.existsSync(tempDir)) {
      fs.mkdirSync(tempDir);
    }
    const tempFilePath = path.join(tempDir, tempFileName);
    fs.writeFileSync(tempFilePath, 'Dummy content for testing.');
    await processFileViaStorage(tempFilePath);
    fs.unlinkSync(tempFilePath);
    fs.rmdirSync(tempDir);
  });

  await test.step(`Then exception should have 1 record with The file failed file validation. Check the file Exceptions blob store. and rule id 0`, async () => {
    await validateSqlDatabaseFromAPI(request, testData.checkInDatabase);
  });


});


test('@DTOSS-7586-01 @regression @e2e @epic1-high-priority @kt Verify Successful Data Processing and Storage in Cohort Manager for Add', async ({ request, testData }) => {
  await test.step(`Given database does not contain 1 ADD record that will be processed`, async () => {
    await cleanupDatabaseFromAPI(request, testData.nhsNumbers);
  });

  await test.step(`When 1 ADD participant is processed via storage`, async () => {
    await processFileViaStorage(testData.runTimeParquetFile);
  });

  await test.step(`Then NHS Numbers should be should be updated in the participants table
                   AND 1 record in cohort table
                   AND no records in exception table`, async () => {
    await validateSqlDatabaseFromAPI(request, testData.checkInDatabase);
  });
});


