import { expect, test } from '../../fixtures/test-fixtures';
import { createParquetFromJson } from '../../../parquet/parquet-multiplier';
import { getApiTestData, processFileViaStorage, validateSqlDatabaseFromAPI, cleanupDatabaseFromAPI } from '../../steps/steps';
import { getRecordsFromParticipantManagementService, sendHttpPOSTCall, sendHttpGet} from '../../../api/distributionService/bsSelectService';
import { TestHooks } from '../../hooks/test-hooks';
import { APIRequestContext, TestInfo } from '@playwright/test';
import { config } from '../../../config/env';
import { getRecordsFromExceptionService } from '../../../api/dataService/exceptionService';

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
    
    var nhsNumberFromPds = 0;
    var familyName = "";
    var dateOfBirth = "";

    await test.step(`Then participant should be in the participant management table`, async () => {
      
        await validateSqlDatabaseFromAPI(request, addValidations);
    });
    

    // Call the block participant function
    await test.step(`Go to PDS and get the participant data for the blocking of a participant that already exists in the database`, async () => {
        // Call the block participant function
        var url = `${config.createPDSDemographic}${config.createPDSDemographicPath}?nhsNumber=${nhsNumbers[0 ]}`;
      //var body = JSON.stringify({ NhsNumber: parseInt(nhsNumbers[0]), FamilyName: inputParticipantRecord[0].family_name, DateOfBirth: inputParticipantRecord[0].date_of_birth })

        var response = await sendHttpGet(url)
        expect(response.status).toBe(200)

        var json = await response.json();
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
      var subscribeToNemsResponse = await sendHttpPOSTCall(`${config.SubToNems}${config.SubToNemsPath}?nhsNumber=${nhsNumbers[0]}`, "");
      
      expect(subscribeToNemsResponse.status).toBe(200);
    });

    await test.step(`Send block to existing participant and make sure they are blocked`, async () => {
      
      var url = `${config.endpointBsSelectUpdateBlockFlag}${config.routeBsSelectBlockParticipant}`;
      var body = JSON.stringify(blockPayload);
      var response = await sendHttpPOSTCall(url, body);
      expect(response.status).toBe(200);
    });


     await test.step(`the participant has been blocked`, async () => {
      let blocked = false;
      while(!blocked) {
          const resp = await getRecordsFromParticipantManagementService(request);
          if (resp?.data?.[0]?.BlockedFlag === 1) {
            blocked = true;
          }
          console.log(`Waiting for participant to be blocked...`);
          await new Promise(res => setTimeout(res, 2000));
        }
        expect(blocked).toBe(true);
    });

   


    await test.step(`send the participant again and expect the participant has not been added and it has not been validated and there is a error in the database that shows that participant is blocked `, async () => {

      const parquetFile = await createParquetFromJson(nhsNumbers, inputParticipantRecord, testFilesPath);
      
      await processFileViaStorage(parquetFile);

      let stopCheckingForValidations = false;
      var validationExceptions;
      while(!stopCheckingForValidations)
      {
          const responseFromExceptions = await getRecordsFromExceptionService(request);
          if(responseFromExceptions.data !== null)
          {
            validationExceptions = responseFromExceptions.data
            stopCheckingForValidations=true  
          }
          console.log(`waiting for exception for participant blocked to be added to exception table...`);
          await new Promise(res => setTimeout(res, 2000));
      }

      var getUrl = `${config.endpointParticipantManagementDataService}api/${config.participantManagementService}`;
      var response = await sendHttpGet(getUrl);

      

      var cohortDistributionServiceUrl = `${config.endpointCohortDistributionDataService}api/${config.cohortDistributionService}`

      var response = await sendHttpGet(cohortDistributionServiceUrl);
      var jsonResponse = await response.json();

      expect(
              response.status === 200 
              && jsonResponse.length === 1 
              && validationExceptions.length === 1 
              && validationExceptions[0].RuleDescription === "Participant is blocked"
            ).toBeTruthy()
    });
  });
});
