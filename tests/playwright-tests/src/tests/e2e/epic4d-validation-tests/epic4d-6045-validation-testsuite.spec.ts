import { test, testWithAmended } from '../../fixtures/test-fixtures';
import { processFileViaStorage, validateSqlDatabaseFromAPI, verifyBlobExists } from '../../steps/steps';
import { TestHooks } from '../../hooks/test-hooks';

test.describe('@regression @e2e @epic4d-validation-tests Tests', () => {
  TestHooks.setupAllTestHooks();

  test('@DTOSS-9492-01 - Verify participant data meets the 3 conditions to trigger Rule 3602 - AC01- ADD', {
    annotation: {
      type: 'Requirement',
      description: 'Tests - https://nhsd-jira.digital.nhs.uk/browse/DTOSS-6045',
    },
  }, async ({ request, testData }) => {

    await test.step(`Then a manual exception will be raised via the Exception Handling Service`, async () => {
      await validateSqlDatabaseFromAPI(request, testData.checkInDatabase);
    });
  });

  testWithAmended('@DTOSS-9493-01 - Verify participant data meets the 3 conditions to trigger Rule 3602 - AC01- AMENDED', {
    annotation: {
      type: 'Requirement',
      description: 'Tests - https://nhsd-jira.digital.nhs.uk/browse/DTOSS-6045',
    },
  }, async ({ request, testData }) => {

    await verifyBlobExists('Verify ProcessCaasFile data file', testData.runTimeParquetFileAdd);

    await test.step(`Given 3 ADD participants are processed to cohort`, async () => {
      await validateSqlDatabaseFromAPI(request, testData.checkInDatabaseAdd);
    });

    await test.step(`When same ADD participant record is AMENDED via storage for ${testData.nhsNumberAmend}`, async () => {
      await processFileViaStorage(testData.runTimeParquetFileAmend);
    });

    await test.step(`Then a manual exception will be raised via the Exception Handling Service`, async () => {
      await validateSqlDatabaseFromAPI(request, testData.checkInDatabaseAmend);
    });

  });

  test('@DTOSS-9494-01 - Verify that exception is raised if one or more conditions are FALSE (CP - English - BAA, PCPC - NOT NULL - E85121) - AC02 - ADD', {
    annotation: {
      type: 'Requirement',
      description: 'Tests - https://nhsd-jira.digital.nhs.uk/browse/DTOSS-6045',
    },
  }, async ({ request, testData }) => {

      await test.step(`Then a manual exception will be raised via the Exception Handling Service`, async () => {
        await validateSqlDatabaseFromAPI(request, testData.checkInDatabase);
      });
    });

    testWithAmended('@DTOSS-9495-01 - Verify participant data meets the 3 conditions to trigger Rule 3602 - AC01- AMENDED', {
      annotation: {
        type: 'Requirement',
        description: 'Tests - https://nhsd-jira.digital.nhs.uk/browse/DTOSS-6045',
      },
    }, async ({ request, testData }) => {

      await verifyBlobExists('Verify ProcessCaasFile data file', testData.runTimeParquetFileAdd);

      await test.step(`Given 3 ADD participants are processed to cohort`, async () => {
        await validateSqlDatabaseFromAPI(request, testData.checkInDatabaseAdd);
      });

      await test.step(`When same ADD participant record is AMENDED via storage for ${testData.nhsNumberAmend}`, async () => {
        await processFileViaStorage(testData.runTimeParquetFileAmend);
      });

      await test.step(`Then a manual exception will be raised via the Exception Handling Service`, async () => {
        await validateSqlDatabaseFromAPI(request, testData.checkInDatabaseAmend);
      });

    });

 });
