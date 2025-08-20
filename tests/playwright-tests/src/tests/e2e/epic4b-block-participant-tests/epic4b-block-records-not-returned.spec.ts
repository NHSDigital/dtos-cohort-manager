import { expect, test } from '../../fixtures/test-fixtures';
import { createParquetFromJson } from '../../../parquet/parquet-multiplier';
import { getApiTestData, processFileViaStorage, validateSqlDatabaseFromAPI, cleanupDatabaseFromAPI } from '../../steps/steps';
import { getRecordsFromParticipantManagementService} from '../../../api/distributionService/bsSelectService';
import { TestHooks } from '../../hooks/test-hooks';
import { APIRequestContext, TestInfo } from '@playwright/test';
import { config } from '../../../config/env';
import { getRecordsFromExceptionService } from '../../../api/dataService/exceptionService';

annotation: [{
  type: 'Requirement',
  description: 'Tests - https://nhsd-jira.digital.nhs.uk/browse/DTOSS-2622'
}]

test.describe('@regression @e2e @epic4b-block-tests @smoke Tests', async () => {
  TestHooks.setupAllTestHooks();

  test('@DTOSS-7615-01 - AC1 - Verify participant is deleted from CohortDistributionDataService', async ({ request }: { request: APIRequestContext }, testInfo: TestInfo) => {
    // Arrange: Get test data
    const [addValidations, addInputParticipantRecord, nhsNumbers, addTestFilesPath] = await getApiTestData(testInfo.title, 'ADD_BLOCKED');
    const nhsNumber = nhsNumbers[0];
    await cleanupDatabaseFromAPI(request, [nhsNumber]);

    // Add the participant (if needed)
    const addParquetFile = await createParquetFromJson(nhsNumbers, addInputParticipantRecord, addTestFilesPath);
    await processFileViaStorage(addParquetFile);

     let nhsNumberFromPds = 0;
    let familyName = "";
    let dateOfBirth = "";

  

    await validateSqlDatabaseFromAPI(request, addValidations);

       // Call the block participant function
    await test.step(`Go to PDS and get the participant data for the blocking of a participant that already exists in the database`, async () => {
        // Call the block participant function
        let url = `${config.createPDSDemographic}${config.createPDSDemographicPath}?nhsNumber=${nhsNumbers[0]}`;

        let response = await sendHttpGet(url)
        expect(response.status).toBe(200)

        let json = await response.json();
        nhsNumberFromPds = json["NhsNumber"];
        familyName = json["FamilyName"]
        dateOfBirth = json["DateOfBirth"];

        expect(nhsNumberFromPds).toBeDefined()
    });

    const blockPayload = {
      NhsNumber: Number(nhsNumberFromPds),
      FamilyName: familyName,
      DateOfBirth: dateOfBirth
    };

    await test.step(`running step to make sure that item can be subscribed to in nems`, async () => {
      let subscribeToNemsResponse = await sendHttpPOSTCall(`${config.SubToNems}${config.SubToNemsPath}?nhsNumber=${nhsNumbers[0]}`, "");

      expect(subscribeToNemsResponse.status).toBe(200);
    });

    await test.step(`Send block to existing participant and make sure they are blocked`, async () => {

      let url = `${config.endpointBsSelectUpdateBlockFlag}${config.routeBsSelectBlockParticipant}`;
      let body = JSON.stringify(blockPayload);
      let response = await sendHttpPOSTCall(url, body);
      expect(response.status).toBe(200);
      expect(response.data == "Participant Has been blocked");
    });
    

  });
});