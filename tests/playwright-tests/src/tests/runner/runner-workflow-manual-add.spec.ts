import { test, request as playwrightRequest, APIRequestContext } from '@playwright/test'
import { cleanupDatabaseFromAPI, getConsolidatedAllTestData, processFileViaStorage, sendParticipantViaSnowAPI, validateSqlDatabaseFromAPI } from '../steps/steps';
import { generateDynamicDateMap, replaceDynamicDatesInJson } from '../../../src/json/json-updater';
import { createParquetFromJson } from '../../parquet/parquet-multiplier';
import { receiveParticipantViaServiceNow, invalidServiceNowEndpoint } from '../../api/distributionService/bsSelectService';

import { runnerBasedEpic4cTestScenariosManualAdd } from '../e2e/epic4c-add-participant-tests/epic4c-testsuite-migrated';
import { ParticipantRecord } from '../../interface/InputData';


// Tests to run based on TEST_TYPE environment variable
let scopedTestScenario = "";
const TEST_TYPE = process.env.TEST_TYPE ?? 'SMOKE';
if (TEST_TYPE == 'RegressionEpic4c') {
  scopedTestScenario = runnerBasedEpic4cTestScenariosManualAdd;
}


let addData = getConsolidatedAllTestData(scopedTestScenario, "ADDMANUAL");
console.log(addData);


let apiContext: APIRequestContext;
test.beforeAll(async () => {
  console.log(`Running Manual Add Tests ⚙️`);
  apiContext = await playwrightRequest.newContext();
  console.log(`Running ${TEST_TYPE} tests with scenario tags: ${scopedTestScenario}`);
  await cleanupDatabaseFromAPI(apiContext, addData.nhsNumbers);


  const dateMap = generateDynamicDateMap();
  const updatedParticipantRecords = replaceDynamicDatesInJson(addData.inputParticipantRecords, dateMap);
  await Promise.all(
    updatedParticipantRecords.map(async (item) => {
      const participantReq = item as ParticipantRecord;
      await sendParticipantViaSnowAPI(apiContext, participantReq);
    }));

  // const runTimeParquetFile = await createParquetFromJson(addData.nhsNumbers, updatedParticipantRecords, addData.testFilesPath, "ADD", false);
  // await processFileViaStorage(runTimeParquetFile);
});

test.afterAll(async () => {
  await apiContext.dispose();
});

console.log('Number of validations:', addData.validations?.length);

addData.validations.forEach((validations) => {
  test(`${validations.meta?.testJiraId} ${validations.meta?.additionalTags}`, {
    annotation: [
      { type: 'TestId', description: validations.meta?.testJiraId ?? '' },
      { type: 'RequirementId', description: validations.meta?.requirementJiraId ?? '' },
      { type: 'manualAdd', description: validations.meta?.requirementJiraId ?? '' },
    ],
  }, async ({ request }) => {
    await validateSqlDatabaseFromAPI(request, [validations]);
  });
});
