import { test, request as playwrightRequest, APIRequestContext } from '@playwright/test'
import { createParquetFromJson } from '../../parquet/parquet-multiplier';
import { cleanupDatabaseFromAPI, getConsolidatedAllTestData, processFileViaStorage, validateSqlDatabaseFromAPI } from '../steps/steps';
import { runnerBasedEpic123TestScenariosAddAmend } from '../e2e/epic123-smoke-tests/epic123-smoke-tests-migrated';
import { runnerBasedEpic1TestScenariosAmend } from '../e2e/epic1-highpriority-tests/epic1-high-priority-testsuite-migrated';
import { runnerBasedEpic2TestScenariosAmend } from '../e2e/epic2-highpriority-tests/epic2-high-priority-testsuite-migrated';
import { runnerBasedEpic3TestScenariosAmend } from '../e2e/epic3-highpriority-tests/epic3-high-priority-testsuite-migrated';
import { runnerBasedEpic3MedTestScenariosAmended } from '../e2e/epic3-medpriority-tests/epic2-med-priority-testsuite-migrated';
import { runnerBasedEpic4dTestScenariosAmend } from '../e2e/epic4d-validation-tests/epic4d-6045-validation-testsuite-migrated';
import { generateDynamicDateMap, replaceDynamicDatesInJson } from '../../../src/json/json-updater';
import { runnerBasedEpic2TestScenariosAmended } from '../e2e/epic2-medpriority-tests/epic2-med-priority-testsuite-migrated';


// Tests to run based on TEST_TYPE environment variable
let scopedTestScenario = "";

const TEST_TYPE = process.env.TEST_TYPE ?? 'SMOKE';
if (TEST_TYPE == 'RegressionEpic1') {
  scopedTestScenario = runnerBasedEpic1TestScenariosAmend;
} else if (TEST_TYPE == 'RegressionEpic2') {
  scopedTestScenario = runnerBasedEpic2TestScenariosAmend;
} else if (TEST_TYPE == 'RegressionEpic2Med') {
  scopedTestScenario = runnerBasedEpic2TestScenariosAmended;
} else if (TEST_TYPE == 'RegressionEpic3') {
  scopedTestScenario = runnerBasedEpic3TestScenariosAmend;
} else if (TEST_TYPE == 'RegressionEpic3Med') {
  scopedTestScenario = runnerBasedEpic3MedTestScenariosAmended;
} else if (TEST_TYPE == 'RegressionEpic4d') {
  scopedTestScenario = runnerBasedEpic4dTestScenariosAmend;
} else {
  scopedTestScenario = runnerBasedEpic123TestScenariosAddAmend;
}

if (!scopedTestScenario) {
  throw new Error("No test scenario tags defined for the current TEST_TYPE. Please check the environment variable.");
}

let addData = getConsolidatedAllTestData(scopedTestScenario, "ADD");
let amendData = getConsolidatedAllTestData(scopedTestScenario, "AMENDED");

let apiContext: APIRequestContext;
test.beforeAll(async () => {
  apiContext = await playwrightRequest.newContext();
  console.log(`Running ${TEST_TYPE} tests with scenario tags: ${scopedTestScenario}`);
  await cleanupDatabaseFromAPI(apiContext, addData.nhsNumbers);
  const runTimeParquetFile = await createParquetFromJson(addData.nhsNumbers, addData.inputParticipantRecords, addData.testFilesPath, "ADD", false);
  await processFileViaStorage(runTimeParquetFile);
  await validateSqlDatabaseFromAPI(apiContext, addData.validations);

  const dateMap = generateDynamicDateMap();
  const updatedParticipantRecordsAmend = replaceDynamicDatesInJson(amendData.inputParticipantRecords, dateMap);


  const runTimeParquetFileAmend = await createParquetFromJson(amendData.nhsNumbers, updatedParticipantRecordsAmend, amendData.testFilesPath, "AMENDED", false);
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
