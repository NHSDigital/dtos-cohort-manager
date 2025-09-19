import { test, request as playwrightRequest, APIRequestContext } from '@playwright/test'
import { cleanupDatabaseFromAPI, getConsolidatedAllTestData, processFileViaStorage, validateSqlDatabaseFromAPI, cleanupWireMock, removeMeshOutboxMappings, enableMeshOutboxFailureInWireMock, resetWireMockMappings, enableMeshOutboxSuccessInWireMock } from '../steps/steps';
import { config } from '../../config/env';
import { sendHttpPOSTCall } from '../../api/core/sendHTTPRequest';
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
// Filter validations to only those that declare a concrete API endpoint.
// Epic4f managecaas flow does not write to all data services, and some test files omit endpoints.
const dbValidations = (addData.validations || []).filter((v: any) => v?.validations?.apiEndpoint);

let apiContext: APIRequestContext;
test.beforeAll(async () => {
  apiContext = await playwrightRequest.newContext();
  console.log(`Running ${TEST_TYPE} tests with scenario tags: ${scopedTestScenario}`);
  // Clean all services at the start to ensure a consistent baseline
  await cleanupDatabaseFromAPI(apiContext, addData.nhsNumbers);


  const dateMap = generateDynamicDateMap();
  const updatedParticipantRecords = replaceDynamicDatesInJson(addData.inputParticipantRecords, dateMap);

  const runTimeParquetFile = await createParquetFromJson(addData.nhsNumbers, updatedParticipantRecords, addData.testFilesPath, "ADD", false);
  if (runTimeParquetFile && runTimeParquetFile !== 'NO_ROWS_TO_WRITE') {
    await processFileViaStorage(runTimeParquetFile);
  } else {
    console.info('Skipping blob upload: no parquet rows to write for this scenario.');
  }
});

test.afterAll(async () => {
  await apiContext.dispose();
});

dbValidations.forEach((validations: any) => {
  test(`${validations.meta?.testJiraId} ${validations.meta?.additionalTags}`, {
    annotation: [
      { type: 'TestId', description: validations.meta?.testJiraId ?? '' },
      { type: 'RequirementId', description: validations.meta?.requirementJiraId ?? '' },
    ],
  }, async ({ request }) => {
    const endpoint = validations?.validations?.apiEndpoint as string | undefined;
    const isExceptionCheck = typeof endpoint === 'string' && endpoint.includes('ExceptionManagementDataService');
    const fast = isExceptionCheck ? { retries: 3, initialWaitMs: 2000, stepMs: 2000 } : undefined;

    // For the Mesh failure scenario, explicitly create the exception record:
    // - Reset WireMock mappings for outbox
    // - Inject a failure mapping
    // - Call subscribe for the scenario NHS number
    // Then validate the exception exists.
    const tag = validations.meta?.testJiraId ?? '';
    const additional = validations.meta?.additionalTags ?? '';
    const isMeshFailureScenario = isExceptionCheck && (tag.includes('@DTOSS-10704-04') || /Mesh\s+failure/i.test(additional));

    if (isMeshFailureScenario) {
      // Determine NHS number from validations
      let nhs = String(process.env.EPIC4F_04_NHS ?? validations?.validations?.NhsNumber ?? validations?.validations?.NHSNumber ?? '');
      if (!nhs) {
        throw new Error('Runner mesh failure scenario requires NhsNumber/NHSNumber in validations');
      }
      // Ensure validations target the NHS we just used
      if (validations?.validations?.NhsNumber !== undefined) validations.validations.NhsNumber = Number(nhs);
      if (validations?.validations?.NHSNumber !== undefined) validations.validations.NHSNumber = Number(nhs);

      // Safe-guard: if WireMock URL not configured, this will still attempt against given URL
      await cleanupWireMock(request);
      await removeMeshOutboxMappings(request);
      await enableMeshOutboxFailureInWireMock(request, 500);

      // Build subscribe URL and call
      const base = config.ManageCaasSubscribe || config.SubToNems;
      const url = new URL(config.SubToNemsPath, base);
      url.searchParams.set('nhsNumber', nhs);
      await sendHttpPOSTCall(url.toString(), '');

      // Proceed to validate DB
      await validateSqlDatabaseFromAPI(request, [validations], fast);

      // Restore mappings for subsequent tests
      await resetWireMockMappings(request);
      await enableMeshOutboxSuccessInWireMock(request);
    } else {
      await validateSqlDatabaseFromAPI(request, [validations], fast);
    }
  });
});
