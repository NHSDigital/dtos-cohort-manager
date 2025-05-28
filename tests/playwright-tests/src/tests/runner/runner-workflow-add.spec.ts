
import { test } from '@playwright/test'
import { cleanupDatabaseFromAPI, getConsolidatedAllTestData, processFileViaStorage, validateSqlDatabaseFromAPI } from '../steps/steps';
import { createParquetFromJson } from '../../parquet/parquet-multiplier';

const epic123SmokeTests = "@DTOSS-6256-01|@DTOSS-6406-01|@DTOSS-7960-01";

let addData = getConsolidatedAllTestData(epic123SmokeTests, "ADD");

test.beforeAll(async ({ request }) => {
  await cleanupDatabaseFromAPI(request, addData.nhsNumbers);
  const runTimeParquetFile = await createParquetFromJson(addData.nhsNumbers, addData.inputParticipantRecords, addData.testFilesPath, "ADD", false);
  await processFileViaStorage(runTimeParquetFile);
});

addData.validations.forEach(async (validations) => {

  test(`@runner-workflow-add ${JSON.stringify(validations)}`, {
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

