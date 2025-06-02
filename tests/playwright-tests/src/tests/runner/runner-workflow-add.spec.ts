import { test, request as playwrightRequest, APIRequestContext } from '@playwright/test'
import { cleanupDatabaseFromAPI, getConsolidatedAllTestData, processFileViaStorage, validateSqlDatabaseFromAPI } from '../steps/steps';
import { createParquetFromJson } from '../../parquet/parquet-multiplier';
import { runnerBasedEpic123TestScenariosAdd } from '../e2e/epic123-smoke-tests/epic123-smoke-tests-migrated.spec';

// Test Scenario Tags
const scopedTestScenario = runnerBasedEpic123TestScenariosAdd;



let apiContext: APIRequestContext;

let addData = getConsolidatedAllTestData(scopedTestScenario, "ADD");

test.beforeAll(async () => {
  apiContext = await playwrightRequest.newContext();
  await cleanupDatabaseFromAPI(apiContext, addData.nhsNumbers);
  const runTimeParquetFile = await createParquetFromJson(addData.nhsNumbers, addData.inputParticipantRecords, addData.testFilesPath, "ADD", false);
  await processFileViaStorage(runTimeParquetFile);
});

test.afterAll(async () => {
  await apiContext.dispose();
});

addData.validations.forEach((validations) => {
  test(`${validations.meta?.testJiraId} ${validations.meta?.additionalTags}`, {
    annotation: [
      { type: 'TestId', description: validations.meta?.testJiraId ?? '' },
      { type: 'RequirementId', description: validations.meta?.requirementJiraId ?? '' },
    ],
  }, async ({ request }) => {
    await validateSqlDatabaseFromAPI(request, [validations]);
  });
});

