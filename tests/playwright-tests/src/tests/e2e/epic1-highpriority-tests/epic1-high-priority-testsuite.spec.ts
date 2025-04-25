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
});
