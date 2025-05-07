import { test } from '../../fixtures/test-fixtures';
import { cleanupDatabaseFromAPI, processFileViaStorage, validateSqlDatabaseFromAPI } from "../../steps/steps";


test.only('@DTOSS-6326-01 - Transformation - Invalid Flag triggers Reason for Removal logic - should apply correct transformations when invalidFlag is true', {
  tag: ['@regression @e2e', '@epic3-high-priority'],
  annotation: {
    type: 'Requirement',
    description: 'Tests - https://nhsd-jira.digital.nhs.uk/browse/DTOSS-5396',
  },
}, async ({ request, testData }) => {

  await cleanupDatabaseFromAPI(request, testData.nhsNumbers);
  await processFileViaStorage(testData.runTimeParquetFile);

  let checkInDatabaseRunTime = testData.checkInDatabase;

  checkInDatabaseRunTime = checkInDatabaseRunTime.map((record: any) => {
    if (record.validations.ReasonForRemovalDate ) {
      record.validations.ReasonForRemovalDate  = new Date().toISOString().split("T")[0] + "T00:00:00";
    }
    return record;
  });

  await validateSqlDatabaseFromAPI(request, checkInDatabaseRunTime);


});

