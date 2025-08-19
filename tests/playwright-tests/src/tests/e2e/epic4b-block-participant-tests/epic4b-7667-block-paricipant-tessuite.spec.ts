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
    });


     await test.step(`the participant has been blocked`, async () => {
      let blocked = false;
      for(let i =0; i<10; i++) {
          const resp = await getRecordsFromParticipantManagementService(request);
          if (resp?.data?.[0]?.BlockedFlag === 1) {
            blocked = true;
            break;
          }
          console.log(`Waiting for participant to be blocked...(${i}/10)`);
          await new Promise(res => setTimeout(res, 2000));
        }
        expect(blocked).toBe(true);
    });

   


    await test.step(`send the participant again and expect the participant has not been added and it has not been validated and there is a error in the database that shows that participant is blocked `, async () => {

      const parquetFile = await createParquetFromJson(nhsNumbers, inputParticipantRecord, testFilesPath);
      
      await processFileViaStorage(parquetFile);

      let validationExceptions;
      for(let i =0; i<10; i++)
      {
          const responseFromExceptions = await getRecordsFromExceptionService(request);
          if(responseFromExceptions.data !== null)
          {
            validationExceptions = responseFromExceptions.data
            break;
          }
          console.log(`waiting for exception for participant blocked to be added to exception table...({${i}/10)`);
          await new Promise(res => setTimeout(res, 2000));
      }

      let getUrl = `${config.endpointParticipantManagementDataService}api/${config.participantManagementService}`;
      var response = await sendHttpGet(getUrl);

      

      let cohortDistributionServiceUrl = `${config.endpointCohortDistributionDataService}api/${config.cohortDistributionService}`

      var response = await sendHttpGet(cohortDistributionServiceUrl);
      var jsonResponse = await response.json();

      expect(
              response.status === 200 
              && jsonResponse.length === 1 
              && validationExceptions.length === 1 
              && validationExceptions[0].RuleDescription === "Participant is blocked"
            ).toBeTruthy();
    });
  });
});
