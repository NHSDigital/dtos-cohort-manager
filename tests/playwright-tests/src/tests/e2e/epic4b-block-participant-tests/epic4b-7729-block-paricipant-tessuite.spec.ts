import { expect, test } from '../../fixtures/test-fixtures';
import { createParquetFromJson } from '../../../parquet/parquet-multiplier';
import { getApiTestData, processFileViaStorage, validateSqlDatabaseFromAPI, cleanupDatabaseFromAPI } from '../../steps/steps';
import { getRecordsFromParticipantManagementService} from '../../../api/distributionService/bsSelectService';
import { TestHooks } from '../../hooks/test-hooks';
import { APIRequestContext, TestInfo } from '@playwright/test';
import { config } from '../../../config/env';
import { getRecordsFromExceptionService } from '../../../api/dataService/exceptionService';
import { sendHttpGet, sendHttpPOSTCall } from '../../../api/core/sendHTTPRequest';

annotation: [{
  type: 'Requirement',
  description: 'Tests - https://nhsd-jira.digital.nhs.uk/browse/DTOSS-7667'
}]

test.describe('@regression @e2e @epic4b-block-tests @smoke Tests', async () => {
  TestHooks.setupAllTestHooks();

  test('@DTOSS-7667-01 - AC1 - Verify participant is deleted from CohortDistributionDataService', async ({ request }: { request: APIRequestContext }, testInfo: TestInfo) => {
    // Arrange: Get test data
    const [addValidations, inputParticipantRecord, nhsNumbers, testFilesPath] = await getApiTestData(testInfo.title, 'ADD_BLOCKED');
    const nhsNumber = nhsNumbers[0];
    await cleanupDatabaseFromAPI(request, [nhsNumber]);


    const parquetFile = await createParquetFromJson(nhsNumbers, inputParticipantRecord, testFilesPath);

    await test.step(`When I ADD participant is processed via storage ready to be blocked by the block function`, async () => {
      await processFileViaStorage(parquetFile);
    });
    
    let nhsNumberFromPds = 0;
    let familyName = "";
    let dateOfBirth = "";

    await test.step(`Then participant should be in the participant management table`, async () => {
      
        await validateSqlDatabaseFromAPI(request, addValidations);
    });
    

    // Call the block participant function
    await test.step(`Go to PDS and get the participant data when PDS returns 500 they are unable to be blocked as they don't exist as a participant in pds`, async () => {
        // Call the block participant function
        let url = `${config.createPDSDemographic}${config.createPDSDemographicPath}?nhsNumber=${nhsNumbers[0]}`;

        let response = await sendHttpGet(url)
        expect(response.status).toBe(404);
    });

    const blockPayload = {
      NhsNumber: Number(nhsNumber),
      FamilyName: "cherry",
      DateOfBirth: "2024-01-01"
    };

   

    await test.step(`Send block to existing participant and make sure they are blocked`, async () => {
      
      let url = `${config.endpointBsSelectUpdateBlockFlag}${config.routeBsSelectBlockParticipant}`;
      let body = JSON.stringify(blockPayload);
      let response = await sendHttpPOSTCall(url, body);
      expect(response.status).toBe(400);
    });
  });
});
