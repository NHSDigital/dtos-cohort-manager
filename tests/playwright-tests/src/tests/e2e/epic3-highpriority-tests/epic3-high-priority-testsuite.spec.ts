import { getRecordsFromExceptionService } from '../../../api/dataService/exceptionService';
import { composeValidators, expectStatus, validateResponseByStatus } from '../../../api/responseValidators';
import { test, testWithAmended } from '../../fixtures/test-fixtures';
import { TestHooks } from '../../hooks/test-hooks';
import { processFileViaStorage, validateSqlDatabaseFromAPI } from "../../steps/steps";


test.describe('@regression @e2e @epic3-high-priority Tests', () => {

  TestHooks.setupAllTestHooks();

  test('@DTOSS-6326-01 - Transformation - Invalid Flag triggers Reason for Removal logic - should apply correct transformations when invalidFlag is true', {
    annotation: {
      type: 'Requirement',
      description: 'Tests - https://nhsd-jira.digital.nhs.uk/browse/DTOSS-5396',
    },
  }, async ({ request, testData }) => {

    await test.step('Then Reason for Removal is set to ORR, Primary Care Provider to null, and  Reason for Removal Date to todays date', async () => {
      let checkInDatabaseRunTime = testData.checkInDatabase;
      checkInDatabaseRunTime = checkInDatabaseRunTime.map((record: any) => {
        if (record.validations.ReasonForRemovalDate) {
          record.validations.ReasonForRemovalDate = new Date().toISOString().split("T")[0] + "T00:00:00";
        }
        return record;
      });
      await validateSqlDatabaseFromAPI(request, checkInDatabaseRunTime);
    });
  });

  testWithAmended('@DTOSS-5596-01 - Transformation - does not trigger removal logic when Reason for Removal is NOT - RDR, RDI, RPR', {
    annotation: {
      type: 'Requirement',
      description: 'Tests - https://nhsd-jira.digital.nhs.uk/browse/DTOSS-4771',
    },
  }, async ({ request, testData }) => {

    await test.step('And ADD participants are processed successfully', async () => {
      await validateSqlDatabaseFromAPI(request, testData.checkInDatabaseAdd);
    });

    await test.step('And there should be transformation exceptions rule trigger for ADD participant', async () => {
      const records = await getRecordsFromExceptionService(request);

      const genericValidations = composeValidators(
        expectStatus(204),
        validateResponseByStatus()
      );
      await genericValidations(records);

    });
    await test.step('Then removal logic should not be triggered, and Reason for Removal should be null', async () => {
      await processFileViaStorage(testData.runTimeParquetFileAmend);
      await validateSqlDatabaseFromAPI(request, testData.checkInDatabaseAmend);
    });

    await test.step('And there should be transformation exceptions rule trigger for AMENDED participant', async () => {
      const records = await getRecordsFromExceptionService(request);

      const genericValidations = composeValidators(
        expectStatus(204),
        validateResponseByStatus()
      );
      await genericValidations(records);

    });
  });

  testWithAmended('@DTOSS-5801-01 @Implement Validate Amend fields reason for removal as DEA and date of death empty', async ({ request, testData }) => {


    await test.step(`When ADD participant is processed via storage`, async () => {
      await processFileViaStorage(testData.runTimeParquetFileAdd);
    });

    await test.step(`Then ADD record should be updated in the cohort`, async () => {
      await validateSqlDatabaseFromAPI(request, testData.checkInDatabaseAdd);
    });

    await test.step(`When same ADD participant record is AMENDED via storage for ${testData.nhsNumberAmend}`, async () => {
      await processFileViaStorage(testData.runTimeParquetFileAmend);
    });

    await test.step(`Then the record should end up in exception management`, async () => {
      await validateSqlDatabaseFromAPI(request, testData.checkInDatabaseAmend);
    });
  });

  testWithAmended('@DTOSS-5589-01 @Implement Validate Amend fields reason for removal as null and date of death present', async ({ request, testData }) => {


    await test.step(`When ADD participant is processed via storage`, async () => {
      await processFileViaStorage(testData.runTimeParquetFileAdd);
    });

    await test.step(`Then ADD record should be updated in the cohort`, async () => {
      await validateSqlDatabaseFromAPI(request, testData.checkInDatabaseAdd);
    });

    await test.step(`When same ADD participant record is AMENDED via storage for ${testData.nhsNumberAmend}`, async () => {
      await processFileViaStorage(testData.runTimeParquetFileAmend);
    });

    await test.step(`Then the record should end up in exception management`, async () => {
      await validateSqlDatabaseFromAPI(request, testData.checkInDatabaseAmend);
    });
  });

  testWithAmended('@DTOSS-5407-01 @Implement Validate Amend fields reason for removal as invalid and date of death present', async ({ request, testData }) => {


    await test.step(`When ADD participant is processed via storage`, async () => {
      await processFileViaStorage(testData.runTimeParquetFileAdd);
    });

    await test.step(`Then ADD record should be updated in the cohort`, async () => {
      await validateSqlDatabaseFromAPI(request, testData.checkInDatabaseAdd);
    });

    await test.step(`When same ADD participant record is AMENDED via storage for ${testData.nhsNumberAmend}`, async () => {
      await processFileViaStorage(testData.runTimeParquetFileAmend);
    });

    await test.step(`Then the record should end up in exception management`, async () => {
      await validateSqlDatabaseFromAPI(request, testData.checkInDatabaseAmend);
    });
  });


});

