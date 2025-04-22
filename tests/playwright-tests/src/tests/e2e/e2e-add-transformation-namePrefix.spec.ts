import { test } from '@playwright/test';
import { cleanupDatabaseFromAPI, getTestData, processFileViaStorage, validateSqlDatabaseFromAPI } from '../steps/steps'
import { createParquetFromJson } from '../../parquet/parquet-multiplier';



test.describe.parallel('namePrefix', () => {

  test.only('@DTOSS-8348-01 Verify file upload and cohort distribution process for ADD - namePrefix', {
    tag: ['@regression @e2e', '@ds'],
  },  async ({ request }, testInfo) => {
    const [checkInDatabase, nhsNumbers, parquetFile, inputParticipantRecord, testFilesPath] = await getTestData(testInfo.title, "ADD", true);
    let runTimeParquetFile: string = "";
    if (parquetFile == ""){
      runTimeParquetFile = await createParquetFromJson(nhsNumbers, inputParticipantRecord!, testFilesPath!, "ADD", false);
    }

    await test.step(`Given database does not contain ADD records that will be processed`, async () => {
      await cleanupDatabaseFromAPI(request, nhsNumbers);
    });

    await test.step(`When ADD participants are processed via storage`, async () => {
      await processFileViaStorage(runTimeParquetFile);
    });

    await test.step(`Then NHS Numbers should be should be updated in the cohort`, async () => {
      await validateSqlDatabaseFromAPI(request, checkInDatabase);
    });

  });

});

