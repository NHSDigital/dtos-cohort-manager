import { test, testWithDelete } from '../../fixtures/test-fixtures';
import { processFileViaStorage, validateSqlDatabaseFromAPI, verifyBlobExists } from '../../steps/steps';
import { TestHooks } from '../../hooks/test-hooks';

test.describe('@regression @e2e @epic4d-validation-tests Tests', () => {
  TestHooks.setupAllTestHooks();

  // test('@DTOSS-9328-01 - Verify transformation of the data occurs with RfR code is set to ORR when CaaS sends a delete record to Cohort Manager - AC01- DEL', {
  //   annotation: {
  //     type: 'Requirement',
  //     description: 'Tests - https://nhsd-jira.digital.nhs.uk/browse/DTOSS-6355',
  //   },
  // }, async ({ request, testData }) => {

  //   await test.step(`Then participant processed to cohort`, async () => {
  //     await validateSqlDatabaseFromAPI(request, testData.checkInDatabase);
  //   });
  // });

  testWithDelete('@DTOSS-9328-01 - Verify transformation of the data occurs with RfR code is set to ORR when CaaS sends a delete record to Cohort Manager - AC01- DEL', {
    annotation: {
      type: 'Requirement',
      description: 'Tests - https://nhsd-jira.digital.nhs.uk/browse/DTOSS-6355',
    },
  }, async ({ request, testData }) => {

    await verifyBlobExists('Verify ProcessCaasFile data file', testData.runTimeParquetFileAdd);

    await test.step(`Given ADD participant processed to cohort`, async () => {
      await validateSqlDatabaseFromAPI(request, testData.checkInDatabaseAdd);
    });

    await test.step(`When same ADD participant record is DEL via storage for ${testData.nhsNumberDelete}`, async () => {
      await processFileViaStorage(testData.runTimeParquetFileDelete);
    });

    await test.step(`Then a manual exception will be raised via the Exception Handling Service`, async () => {
      await validateSqlDatabaseFromAPI(request, testData.checkInDatabaseDelete);
    });

  });
});
