import { test, request as playwrightRequest, APIRequestContext } from '@playwright/test'
import { cleanupDatabaseFromAPI, getConsolidatedAllTestData, processFileViaStorage, validateSqlDatabaseFromAPI } from '../steps/steps';
import { createParquetFromJson } from '../../parquet/parquet-multiplier';
import { runnerBasedEpic123TestScenariosAdd } from '../e2e/epic123-smoke-tests/epic123-smoke-tests-migrated';
import { runnerBasedEpic1TestScenariosAdd } from '../e2e/epic1-highpriority-tests/epic1-high-priority-testsuite-migrated';
import { runnerBasedEpic1MedTestScenariosAdd } from '../e2e/epic1-medpriority-tests/epic1-med-priority-testsuite-migrated';
import { runnerBasedEpic2TestScenariosAdd } from '../e2e/epic2-highpriority-tests/epic2-high-priority-testsuite-migrated';
import { runnerBasedEpic2MedTestScenariosAdd } from '../e2e/epic2-medpriority-tests/epic2-med-priority-testsuite-migrated';
import { runnerBasedEpic3TestScenariosAdd } from '../e2e/epic3-highpriority-tests/epic3-high-priority-testsuite-migrated';
import { runnerBasedEpic3MedTestScenariosAdd } from '../e2e/epic3-medpriority-tests/epic3-med-priority-testsuite-migrated';
import { generateDynamicDateMap, replaceDynamicDatesInJson } from '../../../src/json/json-updater';
import { runnerBasedEpic4cTestScenariosAdd } from '../e2e/epic4c-add-participant-tests/epic4c-testsuite-migrated';
import { runnerBasedEpic4dTestScenariosAdd } from '../e2e/epic4d-validation-tests/epic4d-6045-validation-testsuite-migrated';
import { runnerBasedEpic4fTestScenariosAdd } from '../e2e/epic4f-current-posting-tests/epic4f-testsuite-migrated';


// Tests to run based on TEST_TYPE environment variable
let scopedTestScenario = "";

const TEST_TYPE = process.env.TEST_TYPE ?? 'SMOKE';
if (TEST_TYPE == 'RegressionEpic1') {
  scopedTestScenario = runnerBasedEpic1TestScenariosAdd;
} else if (TEST_TYPE == 'RegressionEpic1Med') {
  scopedTestScenario = runnerBasedEpic1MedTestScenariosAdd;
} else if (TEST_TYPE == 'RegressionEpic2') {
  scopedTestScenario = runnerBasedEpic2TestScenariosAdd;
} else if (TEST_TYPE == 'RegressionEpic2Med') {
  scopedTestScenario = runnerBasedEpic2MedTestScenariosAdd;
} else if (TEST_TYPE == 'RegressionEpic3') {
  scopedTestScenario = runnerBasedEpic3TestScenariosAdd;
} else if (TEST_TYPE == 'RegressionEpic3Med') {
  scopedTestScenario = runnerBasedEpic3MedTestScenariosAdd;
} else if (TEST_TYPE == 'RegressionEpic4d') {
  scopedTestScenario = runnerBasedEpic4dTestScenariosAdd;
} else if (TEST_TYPE == 'RegressionEpic4c') {
  scopedTestScenario = runnerBasedEpic4cTestScenariosAdd;
} else if (TEST_TYPE == 'RegressionEpic4f') {
  scopedTestScenario = runnerBasedEpic4fTestScenariosAdd;
} else {
  scopedTestScenario = runnerBasedEpic123TestScenariosAdd;
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


  const dateMap = generateDynamicDateMap();
  const updatedParticipantRecords = replaceDynamicDatesInJson(addData.inputParticipantRecords, dateMap);

  const runTimeParquetFile = await createParquetFromJson(addData.nhsNumbers, updatedParticipantRecords, addData.testFilesPath, "ADD", false);
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
