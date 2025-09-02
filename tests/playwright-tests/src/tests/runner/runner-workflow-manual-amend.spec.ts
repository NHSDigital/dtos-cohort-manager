import { test, request as playwrightRequest, APIRequestContext } from '@playwright/test'
import { cleanupDatabaseFromAPI, cleanupWireMock, getConsolidatedAllTestData, processFileViaStorage, sendParticipantViaSnowAPI, validateServiceNowRequestWithMockServer, validateSqlDatabaseFromAPI } from '../steps/steps';
import { generateDynamicDateMap, replaceDynamicDatesInJson } from '../../json/json-updater';
import { createParquetFromJson } from '../../parquet/parquet-multiplier';
import { runnerBasedEpic4cTestScenariosManualAmend } from '../e2e/epic4c-add-participant-tests/epic4c-testsuite-migrated';
import { ParticipantRecord } from '../../interface/InputData';


// Tests to run based on TEST_TYPE environment variable
let scopedTestScenario = "";
const TEST_TYPE = process.env.TEST_TYPE ?? 'SMOKE';
if (TEST_TYPE == 'RegressionEpic4c') {
  scopedTestScenario = runnerBasedEpic4cTestScenariosManualAmend;
}


let addData = getConsolidatedAllTestData(scopedTestScenario, "ADDMANUAL");
let amendData = getConsolidatedAllTestData(scopedTestScenario, "AMENDMANUAL");

console.log(addData);


let apiContext: APIRequestContext;
test.beforeAll(async () => {
  console.log(`Running Manual Amend Tests ⚙️`);
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

  await validateSqlDatabaseFromAPI(apiContext, addData.validations);

  const updatedParticipantRecordsAmend = replaceDynamicDatesInJson(amendData.inputParticipantRecords, dateMap);


  const runTimeParquetFileAmend = await createParquetFromJson(amendData.nhsNumbers, updatedParticipantRecordsAmend, amendData.testFilesPath, "AMENDED", false);
  await processFileViaStorage(runTimeParquetFileAmend);
});

test.afterAll(async () => {
  await cleanupWireMock(apiContext);
  await apiContext.dispose();
});

console.log('Number of validations:', amendData.validations?.length);

amendData.validations.forEach((validations) => {
  test(`${validations.meta?.testJiraId} ${validations.meta?.additionalTags}`, {
    annotation: [
      { type: 'TestId', description: validations.meta?.testJiraId ?? '' },
      { type: 'RequirementId', description: validations.meta?.requirementJiraId ?? '' }
    ],
  }, async ({ request }) => {
    await validateSqlDatabaseFromAPI(request, [validations]);
  });
});

console.log('Number of ServiceNow request validations:', amendData.serviceNowRequestValidations?.length)

amendData.serviceNowRequestValidations.forEach((validations) => {
  test(`${validations.meta?.testJiraId} ${validations.meta?.additionalTags}`, {
    annotation: [
      { type: 'TestId', description: validations.meta?.testJiraId ?? '' },
      { type: 'RequirementId', description: validations.meta?.requirementJiraId ?? '' }
    ],
  }, async ({ request }) => {
    await validateServiceNowRequestWithMockServer(request, [validations]);
  });
});
