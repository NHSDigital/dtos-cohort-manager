import { getRecordsFromExceptionService } from '../../../api/dataService/exceptionService';
import { getRecordsFromBsSelectRetrieveCohort } from '../../../api/distributionService/bsSelectService';
import { composeValidators, expectStatus, validateResponseByStatus } from '../../../api/responseValidators';
import { expect, test, testWithAmended } from '../../fixtures/test-fixtures';
import { TestHooks } from '../../hooks/test-hooks';
import { processFileViaStorage, validateSqlDatabaseFromAPI } from "../../steps/steps";
import { getRecordsFromCohortDistributionService } from '../../../api/dataService/cohortDistributionService';


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

  test('@DTOSS-5560-01 - BS Select - Records are received where Is Extracted is set to 0 with correct nhs number, name and date of birth', {
    annotation: {
      type: 'Requirement',
      description: 'Tests - https://nhsd-jira.digital.nhs.uk/browse/DTOSS-3650',
    },
  }, async ({ request, testData }) => {

    await test.step('Then processed ADD participant should be received using bs select get request where is extracted = 0', async () => {
      await validateSqlDatabaseFromAPI(request, testData.checkInDatabase);
      const ExpectedRowCount = 1;

      const response = await getRecordsFromBsSelectRetrieveCohort(request, { rowCount: 10, screeningServiceId: 1 });
      expect(response.data.length).toBe(ExpectedRowCount);

    });

    await test.step('And Is Extracted flag is set to 1', async () => {
      const response = await getRecordsFromCohortDistributionService(request);
      const firstRecord = response.data.find(() => true);
      expect(firstRecord?.IsExtracted).toBe(1);
    });
  });
});

