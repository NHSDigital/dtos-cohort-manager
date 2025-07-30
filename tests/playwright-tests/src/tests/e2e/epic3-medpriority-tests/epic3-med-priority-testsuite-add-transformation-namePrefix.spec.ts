import { test } from '@playwright/test';
import { cleanupDatabaseFromAPI, getCheckInDataBaseValidations, getTestData, processFileViaStorage, validateSqlDatabaseFromAPI } from '../../steps/steps'
import { createParquetFromJson } from '../../../parquet/parquet-multiplier';


let testCaseBuilder: any[] = getCheckInDataBaseValidations("@DTOSS-5556-01")


test.beforeAll(async ({ request }, testInfo) => {

  const [, nhsNumbers, parquetFile, inputParticipantRecord, testFilesPath] = await getTestData(testInfo.title, "ADD", true);

  let runTimeParquetFile: string = "";
  if (!parquetFile) {
    runTimeParquetFile = await createParquetFromJson(nhsNumbers, inputParticipantRecord!, testFilesPath!, "ADD", false);
  }

  await test.step(`Given database does not contain ADD records that will be processed`, async () => {
    await cleanupDatabaseFromAPI(request, nhsNumbers);
  });
  await test.step(`When ADD participants are processed via storage`, async () => {
    await processFileViaStorage(runTimeParquetFile);
  });

});

testCaseBuilder.forEach(async (validations) => {

  const testScenarioName = await buildTestScenarioName(validations);
  test(`@DTOSS-5556-01 @regression @e2e @epic3-med-priority - ${testScenarioName}`, async ({ request }) => {
    await validateSqlDatabaseFromAPI(request, [validations]);
  });
});


async function buildTestScenarioName(validations: any) {
  const testScenario = validations.validations;
  if (testScenario.NamePrefix == undefined) {
    return `Verify RuleId as ${testScenario.RuleId} & description as ${testScenario.RuleDescription} for NHS participant ${testScenario.NhsNumber}`
  } else {
    return `Verify NamePrefix as ${testScenario.NamePrefix} for NHS participant ${testScenario.NHSNumber}`
  }
}







