import { test } from '../../fixtures/test-fixtures';
import { TestHooks } from '../../hooks/test-hooks';
import { validateSqlDatabaseFromAPI } from "../../steps/steps";


test.describe('@regression @e2e @epic3-high-priority Tests', () => {

  TestHooks.setupAddTestHooks();

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
});

