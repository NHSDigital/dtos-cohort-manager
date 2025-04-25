import { test } from '@playwright/test';
import { cleanupDatabaseFromAPI, getTestData, processFileViaStorage, validateSqlDatabaseFromAPI } from '../../steps/steps';
import { createParquetFromJson } from '../../../parquet/parquet-multiplier';

test('@DTOSS-8544-01 @regression @e2e @epic2-high-priority @Implement Validation for Eligibility Flag for Add', async ({ request }, testInfo) => {
  const [checkInDatabase, nhsNumbers, parquetFile, inputParticipantRecord, testFilesPath] = await getTestData(testInfo.title, "ADD", true);

  let runTimeParquetFile: string = "";
  if (!parquetFile) {
    runTimeParquetFile = await createParquetFromJson(nhsNumbers, inputParticipantRecord!, testFilesPath!, "ADD", false);
  }
  await test.step(`Given database does not contain record that will be processed`, async () => {
    await cleanupDatabaseFromAPI(request, nhsNumbers);
  });

  await test.step(`When ADD participant is processed via storage`, async () => {
    await processFileViaStorage(runTimeParquetFile);
  });

  await test.step(`Then NHS Numbers should be updated in the cohort`, async () => {
    await validateSqlDatabaseFromAPI(request, checkInDatabase);
  });

  await test.step(`Then validate eligibility flag is always set to true for ADD`, async () => {
    await validateSqlDatabaseFromAPI(request, checkInDatabase);
  });
});

test('@DTOSS-8544-01 @regression @e2e @epic2-high-priority @Implement Validation for Eligibility Flag for Amended', async ({ request }, testInfo) => {
  const [checkInDatabaseAdd, nhsNumbers, parquetFile, inputParticipantRecord, testFilesPath] = await getTestData(testInfo.title, "ADD", true);

  let runTimeParquetFileAdd: string = "";
  if (!parquetFile) {
    runTimeParquetFileAdd = await createParquetFromJson(nhsNumbers, inputParticipantRecord!, testFilesPath!, "ADD", false);
  }


  await test.step(`Given database does not contain record that will be processed`, async () => {
    await cleanupDatabaseFromAPI(request, nhsNumbers);
  });

  await test.step(`When ADD participant is processed via storage`, async () => {
    await processFileViaStorage(runTimeParquetFileAdd);
  });

  await test.step(`Then ADD record should be updated in the cohort`, async () => {
    await validateSqlDatabaseFromAPI(request, checkInDatabaseAdd);
  });


  const [checkInDatabaseAmend, nhsNumberAmend, parquetFileAmend, inputParticipantRecordAmend, testFilesPathAmend] = await getTestData(testInfo.title, "AMENDED", true);

  let runTimeParquetFileAmend: string = "";
  if (!parquetFileAmend) {
    runTimeParquetFileAmend = await createParquetFromJson(nhsNumberAmend, inputParticipantRecordAmend!, testFilesPathAmend!, "AMENDED", false);
  }

  await test.step(`When same ADD participant record is AMENDED via storage for ${nhsNumberAmend}`, async () => {
    await processFileViaStorage(runTimeParquetFileAmend);
  });

  await test.step(`Then validate eligibility flag is always set to true for AMENDED`, async () => {
    await validateSqlDatabaseFromAPI(request, checkInDatabaseAmend);
  });
});

test('@DTOSS-8534-01 @regression @e2e @epic2-high-priority @Implement validate eligibility flag set to flase for ADD', async ({ request }, testInfo) => {
  const [checkInDatabase, nhsNumbers, parquetFile, inputParticipantRecord, testFilesPath] = await getTestData(testInfo.title, "ADD", true);

  let runTimeParquetFile: string = "";
  if (!parquetFile) {
    runTimeParquetFile = await createParquetFromJson(nhsNumbers, inputParticipantRecord!, testFilesPath!, "ADD", false);
  }

  await test.step(`Given database does not contain record that will be processed`, async () => {
    await cleanupDatabaseFromAPI(request, nhsNumbers);
  });

  await test.step(`When ADD participant is processed via storage`, async () => {
    await processFileViaStorage(runTimeParquetFile);
  });

  await test.step(`When same ADD participant record is AMENDED via storage for ${nhsNumberAmend}`, async () => {
    await processFileViaStorage(parquetFileAmend!);
  });

  await test.step(`Then validate eligibility flag is set to false for ADD`, async () => {
    await validateSqlDatabaseFromAPI(request, checkInDatabase);
  });
});

test('09 @regression @e2e @epic2-high-priority @DTOSS-8535 @Implement validate invalid flag value for Amended', async ({ request }, testInfo) => {
  const [checkInDatabase, nhsNumber, parquetFileAdd] = await getTestData(testInfo.title);
  const [checkInDatabaseAmend, nhsNumberAmend, parquetFileAmend] = await getTestData(testInfo.title, "AMENDED");

  await test.step(`Given database does not contain record that will be processed`, async () => {
    await cleanupDatabaseFromAPI(request, nhsNumber);
  });

  await test.step(`When ADD participant is processed via storage`, async () => {
    await processFileViaStorage(parquetFileAdd!);
  });

  await test.step(`When same ADD participant record is AMENDED via storage for ${nhsNumberAmend}`, async () => {
    await processFileViaStorage(parquetFileAmend!);
  });

  await test.step(`Then validate invalid flag set to flase for AMENDED`, async () => {
    await validateSqlDatabaseFromAPI(request, checkInDatabase);
  });
});

test('10 @regression @e2e @epic2-high-priority @DTOSS-8535 @Implement validate invalid flag value for Amended', async ({ request }, testInfo) => {
  const [checkInDatabase, nhsNumber, parquetFileAdd] = await getTestData(testInfo.title);
  const [checkInDatabaseAmend, nhsNumberAmend, parquetFileAmend] = await getTestData(testInfo.title, "AMENDED");

  await test.step(`Given database does not contain record that will be processed`, async () => {
    await cleanupDatabaseFromAPI(request, nhsNumber);
  });

  await test.step(`When ADD participant is processed via storage`, async () => {
    await processFileViaStorage(parquetFileAdd!);
  });

  await test.step(`When same ADD participant record is AMENDED via storage for ${nhsNumberAmend}`, async () => {
    await processFileViaStorage(parquetFileAmend!);
  });

  await test.step(`Then validate invalid flag set to true for AMENDED`, async () => {
    await validateSqlDatabaseFromAPI(request, checkInDatabase);
  });
});
