import { test } from '@playwright/test';
import { cleanupDatabase, validateSqlData } from '../../database/sqlVerifier';
import { getTestData, processFileViaStorage, validateSqlDatabase } from '../steps/steps'



test.describe('Smoke Tests', () => {
  test('@smoke @DTOSS-6256 Verify file upload and cohort distribution process for ADD', async () => {

    const testFile = "ADD_2_RECORDS_-_CAAS_BREAST_SCREENING_COHORT";
    const [checkInDatabase, nhsNumbers] = await getTestData(`${testFile}.json`);

    await test.step(`Given database does not contain 2 ADD records that will be processed`, async () => {
      await cleanupDatabase(nhsNumbers); //TODO re-think try remove-participant api instead
    });

    await test.step(`When 2 ADD participants are processed via storage`, async () => {
      await processFileViaStorage(`${testFile}.parquet`);
    });

    await test.step(`Then NHS Numbers should be should be updated in the cohort`, async () => {
      await validateSqlDatabase(checkInDatabase.validations);
    });

  });
  test('@smoke @DTOSS-6257 Verify file upload and cohort distribution process for ADD followed by AMENDED records', async () => {

    //TODO move setup to test.before
    const testFileAdd = "ADD1_1B8F53_-_CAAS_BREAST_SCREENING_COHORT";
    const testFileAmend = "AMENDED1_1B8F53_-_CAAS_BREAST_SCREENING_COHORT";
    const [checkInDatabaseAdd, nhsNumbers] = await getTestData(`${testFileAdd}.json`);
    const [checkInDatabaseAmend] = await getTestData(`${testFileAmend}.json`);

    await test.step(`Given database does not contain record that will be processed`, async () => {
      await cleanupDatabase(nhsNumbers); //TODO re-think try remove-participant api instead
    });

    await test.step(`When ADD participant is processed via storage`, async () => {
      await processFileViaStorage(`${testFileAdd}.parquet`);
    });

    await test.step(`Then ADD record should be updated in the cohort`, async () => {
      await validateSqlData(checkInDatabaseAdd.validations);
    });

    await test.step(`When same ADD participant record is AMENDED via storage`, async () => {
      await processFileViaStorage(`${testFileAmend}.parquet`);
    });

    await test.step(`Then AMENDED record name should be updated in the cohort`, async () => {
      await validateSqlDatabase(checkInDatabaseAmend.validations);
    });




  });
  test('@smoke @DTOSS-7960 Verify GP Practice Code Exception, flag in participant management is set to 1', async () => {

    const testFile = "Exception_1B8F53_-_CAAS_BREAST_SCREENING_COHORT";
    const [checkInDatabase, nhsNumbers] = await getTestData(`${testFile}.json`);

    await test.step(`Given database does not contain records that will be processed: ${nhsNumbers}  `, async () => {
      await cleanupDatabase(nhsNumbers); //TODO re-think try remove-participant api instead
    });

    await test.step(`When participants are processed via storage`, async () => {
      await processFileViaStorage(`${testFile}.parquet`);
    });

    await test.step(`Then records should be updated in the cohort`, async () => {
      await validateSqlDatabase(checkInDatabase);
    });
  });
})

