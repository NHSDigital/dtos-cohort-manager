import { test } from '@playwright/test'
import { createParquetFromJson } from '../../parquet/parquet-multiplier';
import { cleanupDatabaseFromAPI, getConsolidatedAllTestData, processFileViaStorage, validateSqlDatabaseFromAPI } from '../steps/steps';

const epic123SmokeTests = "@DTOSS-6257-01|@DTOSS-6407-01";


let addData = getConsolidatedAllTestData(epic123SmokeTests, "ADD");
let amendData = getConsolidatedAllTestData(epic123SmokeTests, "AMENDED");

test.beforeAll(async ({ request }) => {
  await cleanupDatabaseFromAPI(request, addData.nhsNumbers);
  const runTimeParquetFile = await createParquetFromJson(addData.nhsNumbers, addData.inputParticipantRecords, addData.testFilesPath, "ADD", false);
  await processFileViaStorage(runTimeParquetFile);
  await validateSqlDatabaseFromAPI(request, addData.validations)

  const runTimeParquetFileAmend = await createParquetFromJson(amendData.nhsNumbers, amendData.inputParticipantRecords, amendData.testFilesPath, "AMENDED", false);
  await processFileViaStorage(runTimeParquetFileAmend);

});


amendData.validations.forEach(async (validations) => {

  test(`@runner-workflow-amend ${JSON.stringify(validations)}`, {
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

