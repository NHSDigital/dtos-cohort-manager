import { test, request as playwrightRequest, APIRequestContext } from '@playwright/test'
import { createParquetFromJson } from '../../parquet/parquet-multiplier';
import { cleanupDatabaseFromAPI, getConsolidatedAllTestData, processFileViaStorage, validateSqlDatabaseFromAPI } from '../steps/steps';
import { runnerBasedEpic123TestScenariosAddAmend } from '../e2e/epic123-smoke-tests/epic123-smoke-tests-migrated';

// Test Scenario Tags
const smokeTestScenario = runnerBasedEpic123TestScenariosAddAmend;


let addData = getConsolidatedAllTestData(smokeTestScenario, "ADD");
let amendData = getConsolidatedAllTestData(smokeTestScenario, "AMENDED");

let apiContext: APIRequestContext;

test.beforeAll(async () => {
  apiContext = await playwrightRequest.newContext();
  await cleanupDatabaseFromAPI(apiContext, addData.nhsNumbers);
  const runTimeParquetFile = await createParquetFromJson(addData.nhsNumbers, addData.inputParticipantRecords, addData.testFilesPath, "ADD", false);
  await processFileViaStorage(runTimeParquetFile);
  await validateSqlDatabaseFromAPI(apiContext, addData.validations);

  const runTimeParquetFileAmend = await createParquetFromJson(amendData.nhsNumbers, amendData.inputParticipantRecords, amendData.testFilesPath, "AMENDED", false);
  await processFileViaStorage(runTimeParquetFileAmend);
});

test.afterAll(async () => {
  await apiContext.dispose();
});


amendData.validations.forEach((validations) => {

  test(`${validations.meta?.testJiraId} ${validations.meta?.additionalTags}`, {
    annotation: [{
      type: 'TestId',
      description: validations.meta?.testJiraId ?? '',
    }, {
      type: 'RequirementId',
      description: validations.meta?.requirementJiraId ?? '',
    }],
  }, async ({ request }) => {
    await validateSqlDatabaseFromAPI(request, [validations]);
  });
});

