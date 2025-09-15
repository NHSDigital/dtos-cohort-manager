import { test, request as playwrightRequest, APIRequestContext } from '@playwright/test'
import { createParquetFromJson } from '../../parquet/parquet-multiplier';
import { cleanupDatabaseFromAPI, getConsolidatedAllTestData, processFileViaStorage, validateSqlDatabaseFromAPI } from '../steps/steps';
import { generateDynamicDateMap, replaceDynamicDatesInJson } from '../../../src/json/json-updater';
import { fail } from 'assert';
import { TestTypePicker } from './TestTypePicker';

// Tests to run based on TEST_TYPE environment variable


let scopedTestScenario = "";
const TEST_TYPE = process.env.TEST_TYPE ?? 'SMOKE';

scopedTestScenario = TestTypePicker(TEST_TYPE)

if (!scopedTestScenario) {
  console.error("No test scenario tags defined for the current TEST_TYPE. Please check the environment variable.");
  fail;
}

let addData = getConsolidatedAllTestData(scopedTestScenario, "ADD");
let amendData = getConsolidatedAllTestData(scopedTestScenario, "AMENDED");

let apiContext: APIRequestContext;
test.beforeAll(async () => {
  setTimeout(() => {
  console.log("running tests from here");
      }, 5000
  );
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



const delay = (ms: number | undefined) => new Promise(res => setTimeout(res, ms));