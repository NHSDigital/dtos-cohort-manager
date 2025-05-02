import { testWithAmended2 } from '../../fixtures/test-fixtures';
import { cleanupDatabaseFromAPI, processFileViaStorage, validateSqlDatabaseFromAPI } from '../../steps/steps';


testWithAmended2.describe('@regression @e2e @epic3-high-priority', () => {

  testWithAmended2.beforeEach(async ({ request, testData }) => {
    await cleanupDatabaseFromAPI(request, testData.nhsNumbers);
    await processFileViaStorage(testData.runTimeParquetFileAdd);
    await validateSqlDatabaseFromAPI(request, testData.checkInDatabaseAdd);
    await processFileViaStorage(testData.runTimeParquetFileAmend);
    await validateSqlDatabaseFromAPI(request, testData.checkInDatabaseAmend);
    await processFileViaStorage(testData.runTimeParquetFileAmend2);
  });

  testWithAmended2('@DTOSS-5410-01 Reason for removal Rule 4 - ParticipantNotRegisteredToGPWithReasonForRemoval', async ({ request, testData }) => {
    await validateSqlDatabaseFromAPI(request, testData.checkInDatabaseAmend2);

  });
});










