import { test, request as playwrightRequest, APIRequestContext } from '@playwright/test'
import { cleanupDatabaseFromAPI, getConsolidatedAllTestData, processFileViaStorage, validateSqlDatabaseFromAPI, verifyBlobExists } from '../steps/steps';
import { createParquetFromJson } from '../../parquet/parquet-multiplier';
import { runnerBasedEpic3TestScenariosAdd } from '../e2e/epic3-highpriority-tests/epic3-high-priority-testsuite-migrated';

// Test Scenario Tags
const smokeTestScenario = runnerBasedEpic3TestScenariosAdd;
const regressionTestScenario = runnerBasedEpic3TestScenariosAdd;

// Tets to run based on TEST_TYPE environment variable
let scopedTestScenario = "";

const TEST_TYPE = process.env.TEST_TYPE ?? 'SMOKE';
if (TEST_TYPE == 'Regression') {
  scopedTestScenario = regressionTestScenario;
} else {
  scopedTestScenario = smokeTestScenario;
}

let apiContext: APIRequestContext;
if (!scopedTestScenario) {
  throw new Error("No test scenario tags defined for the current TEST_TYPE. Please check the environment variable.");
} else {
  console.log(`Running ${TEST_TYPE} tests with scenario tags: ${scopedTestScenario}`);
}
let addData = getConsolidatedAllTestData(scopedTestScenario, "ADD");

test.beforeAll(async () => {
  apiContext = await playwrightRequest.newContext();
  await cleanupDatabaseFromAPI(apiContext, addData.nhsNumbers);
  const runTimeParquetFile = await createParquetFromJson(addData.nhsNumbers, addData.inputParticipantRecords, addData.testFilesPath, "ADD", false);
  await verifyBlobExists('Verify ProcessCaasFile data file', runTimeParquetFile);
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

