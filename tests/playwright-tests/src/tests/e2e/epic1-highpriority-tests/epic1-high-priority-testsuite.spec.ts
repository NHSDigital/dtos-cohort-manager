import { test } from '@playwright/test';
import { cleanupDatabaseFromAPI, getTestData, processFileViaStorage, validateSqlDatabaseFromAPI } from '../../steps/steps'
import { createParquetFromJson } from '../../../parquet/parquet-multiplier';



test.describe.parallel('Positive @smoke Tests', () => {

  test('@DTOSS-8519-01 @regression @e2e Verify file upload into participants table for ADD', async ({ request }, testInfo) => {
      const [checkInDatabase, nhsNumbers, parquetFile, inputParticipantRecord, testFilesPath] = await getTestData(testInfo.title, "ADD", true);

      let runTimeParquetFile: string = "";
      if (!parquetFile) {
        runTimeParquetFile = await createParquetFromJson(nhsNumbers, inputParticipantRecord!, testFilesPath!, "ADD", false);
      }
    await test.step(`Given database does not contain 2 ADD records that will be processed`, async () => {
      await cleanupDatabaseFromAPI(request, nhsNumbers);
    });

    await test.step(`When 2 ADD participants are processed via storage`, async () => {
      await processFileViaStorage(runTimeParquetFile);
    });

    await test.step(`Then NHS Numbers should be should be updated in the participants table`, async () => {
      await validateSqlDatabaseFromAPI(request, checkInDatabase);
    });

  })

  test('@DTOSS-8521-01 @regression @e2e @ut @epic1-high-priority @DTOSS-8521 @Verify AMENDED records reach the participant tables', async ({ request }, testInfo) => {
    const [checkInDatabaseAdd, nhsNumbers, parquetFile, inputParticipantRecord, testFilesPath] = await getTestData(testInfo.title, "ADD", true);

    let runTimeParquetFileAdd: string = "";
    if (!parquetFile) {
      runTimeParquetFileAdd = await createParquetFromJson(nhsNumbers, inputParticipantRecord!, testFilesPath!, "ADD", false);
    }


    await test.step(`Given database does not contain record that will be processed`, async () => {
      await cleanupDatabaseFromAPI(request, nhsNumbers);
    });

    await test.step(`When ADD participants is processed via storage`, async () => {
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


    await test.step(`When same ADD participant record is AMENDED via storage`, async () => {
      await processFileViaStorage(runTimeParquetFileAmend!);
    });

    await test.step(`Then ADD record should be updated in the participants management table`, async () => {
      await validateSqlDatabaseFromAPI(request, checkInDatabaseAmend);
    });


    await test.step(`Then AMENDED record name should be updated in the participants demographic table: ${nhsNumberAmend}`, async () => {
      await validateSqlDatabaseFromAPI(request, checkInDatabaseAmend);
    });
  });

});
