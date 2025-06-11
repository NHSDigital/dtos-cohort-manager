import { test, testWithAmended } from '../../fixtures/test-fixtures';
import { processFileViaStorage, validateSqlDatabaseFromAPI, verifyBlobExists } from '../../steps/steps';
import { TestHooks } from '../../hooks/test-hooks';

test.describe('@regression @e2e @epic4d-validation-tests Tests', () => {
  TestHooks.setupAllTestHooks();

  test.only('@DTOSS-94092-01 - Verify participant data meets the 3 conditions to trigger Rule 3602', {
    annotation: {
      type: 'Requirement',
      description: 'Tests - https://nhsd-jira.digital.nhs.uk/browse/DTOSS-6045',
    },
  }, async ({ request, testData }) => {

    await test.step(`Verify exception in exception table`, async () => {
      await validateSqlDatabaseFromAPI(request, testData.checkInDatabase);
    });
  });

  testWithAmended('@DTOSS-9493-01-Verify participant data meets the 3 conditions to trigger Rule 3602 -AMENDED', {
    annotation: {
      type: 'Requirement',
      description: 'Tests - https://nhsd-jira.digital.nhs.uk/browse/DTOSS-6045',
    },
  }, async ({ request, testData }) => {


    await verifyBlobExists('Verify ProcessCaasFile data file', testData.runTimeParquetFileAdd);

    await test.step(`Then ADD record should be updated in the participants demographic table`, async () => {
      await validateSqlDatabaseFromAPI(request, testData.checkInDatabaseAdd);
    });

    await test.step(`When same ADD participant record is AMENDED via storage for ${testData.nhsNumberAmend}`, async () => {
      await processFileViaStorage(testData.runTimeParquetFileAmend);
    });

    await test.step(`Then the record should end up in exception management table`, async () => {
        await validateSqlDatabaseFromAPI(request, testData.checkInDatabaseAmend);
    });

});

});
