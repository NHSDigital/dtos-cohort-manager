import { test, request as playwrightRequest, APIRequestContext } from '@playwright/test'
import { cleanupDatabaseFromAPI, getConsolidatedAllTestData, processFileViaStorage, validateSqlDatabaseFromAPI } from '../steps/steps';
import { createParquetFromJson } from '../../parquet/parquet-multiplier';
import { runnerBasedEpic123TestScenariosAdd } from '../e2e/epic123-smoke-tests/epic123-smoke-tests-migrated';
import { runnerBasedEpic1TestScenariosAdd } from '../e2e/epic1-highpriority-tests/epic1-high-priority-testsuite-migrated';
import { runnerBasedEpic2TestScenariosAdd } from '../e2e/epic2-highpriority-tests/epic2-high-priority-testsuite-migrated';
import { runnerBasedEpic3TestScenariosAdd } from '../e2e/epic3-highpriority-tests/epic3-high-priority-testsuite-migrated';
import { runnerBasedEpic4dTestScenariosAdd } from '../e2e/epic4d-validation-tests/epic4d-6045-validation-testsuite-migrated';

// Test Scenario Tags
const smokeTestScenario = runnerBasedEpic123TestScenariosAdd;
const regressionEpic1TestScenario = runnerBasedEpic1TestScenariosAdd;
const regressionEpic2TestScenario = runnerBasedEpic2TestScenariosAdd;
const regressionEpic3TestScenario = runnerBasedEpic3TestScenariosAdd;
const regressionEpic4dTestScenario = runnerBasedEpic4dTestScenariosAdd;

// Tests to run based on TEST_TYPE environment variable
let scopedTestScenario = "";

const TEST_TYPE = process.env.TEST_TYPE ?? 'SMOKE';
if (TEST_TYPE == 'RegressionEpic1') {
  scopedTestScenario = regressionEpic1TestScenario;
} else if (TEST_TYPE == 'RegressionEpic2') {
  scopedTestScenario = regressionEpic2TestScenario;
} else if (TEST_TYPE == 'RegressionEpic3') {
  scopedTestScenario = regressionEpic3TestScenario;
} else if (TEST_TYPE == 'RegressionEpic4d') {
  scopedTestScenario = regressionEpic4dTestScenario;
} else {
  scopedTestScenario = smokeTestScenario;
}

if (!scopedTestScenario) {
  throw new Error("No test scenario tags defined for the current TEST_TYPE. Please check the environment variable.");
}

let addData = getConsolidatedAllTestData(scopedTestScenario, "ADD");


let apiContext: APIRequestContext;
test.beforeAll(async () => {
  apiContext = await playwrightRequest.newContext();
  console.log(`Running ${TEST_TYPE} tests with scenario tags: ${scopedTestScenario}`);
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
