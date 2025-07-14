import { test, testWithTwoAmendments } from '../../fixtures/test-fixtures';
import { TestHooks } from '../../hooks/test-hooks';
import { processFileViaStorage, validateSqlDatabaseFromAPI } from "../../steps/steps";


test.describe('@regression @e2e @epic2-medium-priority Tests', () => {

  TestHooks.setupAllTestHooks();

  testWithTwoAmendments('@DTOSS-5105-01 @not-runner-based @P1 Validation - Validations_Valid_supplied_ReasonForRemoval_empty_Has_valid_GP_PracticeCode_Amend', {
    annotation: {
      type: 'Requirement',
      description: 'Tests - https://nhsd-jira.digital.nhs.uk/browse/DTOSS-3040',
    },
  }, async ({ request, testData }) => {
    await test.step(`When ADD participant is processed via storage`, async () => {
      await processFileViaStorage(testData.runTimeParquetFileAdd);
    });

    await test.step(`Then ADD record should be updated in the cohort`, async () => {
      await validateSqlDatabaseFromAPI(request, testData.checkInDatabaseAdd);
    });

    await test.step(`When same ADD participant record is AMENDED via storage for ${testData.nhsNumberAmend}`, async () => {
      await processFileViaStorage(testData.runTimeParquetFileAmend);
    });

    await test.step(`Then the record should end up in cohort distribution table`, async () => {
      await validateSqlDatabaseFromAPI(request, testData.checkInDatabaseAmend);
    });

    await test.step(`When same ADD participant record is AMENDED via storage for ${testData.nhsNumberAmend}`, async () => {
      await processFileViaStorage(testData.runTimeParquetFileSecondAmend);

    });

    await test.step(`Then the record should end up in cohort distribution table`, async () => {
      await validateSqlDatabaseFromAPI(request, testData.checkInDatabaseSecondAmend);
    });

  });

  testWithTwoAmendments('@DTOSS-5107-01 Tests Reason for Removal is set as SDL ', {
      annotation: {
        type: 'Requirement',
        description: 'Tests - https://nhsd-jira.digital.nhs.uk/browse/DTOSS-3040',
      },
    }, async ({ request, testData }) => {
      await test.step(`When ADD participant is processed via storage`, async () => {
        await processFileViaStorage(testData.runTimeParquetFileAdd);
      });

      await test.step(`Then ADD record should be updated in the cohort`, async () => {
        await validateSqlDatabaseFromAPI(request, testData.checkInDatabaseAdd);
      });

      await test.step(`When same ADD participant record is AMENDED via storage for ${testData.nhsNumberAmend}`, async () => {
        await processFileViaStorage(testData.runTimeParquetFileAmend);
      });

      await test.step(`Then the record should end up in cohort distribution table`, async () => {
        await validateSqlDatabaseFromAPI(request, testData.checkInDatabaseAmend);
      });

      await test.step(`When same ADD participant record is AMENDED via storage for ${testData.nhsNumberAmend}`, async () => {
        await processFileViaStorage(testData.runTimeParquetFileSecondAmend);

      });

      await test.step(`Then the record should end up in cohort distribution table`, async () => {
        await validateSqlDatabaseFromAPI(request, testData.checkInDatabaseSecondAmend);
      });

    });

});
