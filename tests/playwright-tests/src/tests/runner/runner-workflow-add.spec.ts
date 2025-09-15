import { test, request as playwrightRequest, APIRequestContext } from '@playwright/test'
import { cleanupDatabaseFromAPI, getConsolidatedAllTestData, processFileViaStorage, validateSqlDatabaseFromAPI } from '../steps/steps';
import { createParquetFromJson } from '../../parquet/parquet-multiplier';
import { generateDynamicDateMap, replaceDynamicDatesInJson } from '../../../src/json/json-updater';
import { fail } from 'assert';
import { TestTypePicker } from './TestTypePicker';


// Tests to run based on TEST_TYPE environment variable
const TEST_TYPE = process.env.TEST_TYPE ?? 'SMOKE';
let scopedTestScenario = "";

scopedTestScenario = TestTypePicker(TEST_TYPE)

if (!scopedTestScenario) {
  console.error("No test scenario tags defined for the current TEST_TYPE. Please check the environment variable.");
  fail;
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
