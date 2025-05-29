import { test, request as playwrightRequest, APIRequestContext } from '@playwright/test'
import { cleanupDatabaseFromAPI, getConsolidatedAllTestData, processFileViaStorage, validateSqlDatabaseFromAPI } from '../steps/steps';
import { createParquetFromJson } from '../../parquet/parquet-multiplier';

const epic123SmokeTests = "@DTOSS-6256-01|@DTOSS-6406-01|@DTOSS-7960-01";

let apiContext: APIRequestContext;
let addData = getConsolidatedAllTestData(epic123SmokeTests, "ADD");

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
  test(`@runner-workflow-add ${validations.meta?.additionalTags}`, {
    annotation: [
      { type: 'TestId', description: validations.meta?.testJiraId ?? '' },
      { type: 'RequirementId', description: validations.meta?.requirementJiraId ?? '' },
    ],
  }, async ({ request }) => {
    await validateSqlDatabaseFromAPI(request, [validations]);
  });
});

