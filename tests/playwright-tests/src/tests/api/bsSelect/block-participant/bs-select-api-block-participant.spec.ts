import { test, expect } from '@playwright/test';
import { createParquetFromJson } from '../../../../parquet/parquet-multiplier';
import { getApiTestData, processFileViaStorage, cleanupDatabaseFromAPI, validateSqlDatabaseFromAPI } from '../../../steps/steps';
import { BlockParticipant} from '../../../../api/distributionService/bsSelectService'
import { getRecordsFromParticipantManagementService } from '../../../../api/distributionService/participantService';


test.describe.serial(' @api Positive - Block Participant called', async () => {

  test.only('@DTOSS-7694-01 200 @smoke @api - @TC1_SIT Verify the ability to block a participant', async ({ request }, testInfo) => {

    const [checkInDatabase, inputParticipantRecord, nhsNumbers, testFilesPath] = await getApiTestData(testInfo.title);

    await cleanupDatabaseFromAPI(request, nhsNumbers);

    const parquetFile = await createParquetFromJson(nhsNumbers, inputParticipantRecord, testFilesPath);

    await test.step(`When 1 ADD participant is processed via storage`, async () => {
      await processFileViaStorage(parquetFile);
    });

    await test.step(`Then participant should be in the participant management table`, async () => {
      await validateSqlDatabaseFromAPI(request, checkInDatabase);
    });

    // Call the block participant function
    await test.step(`When BlockParticipant function is invoked`, async () => {
      const blockPayload = {
        NhsNumber: nhsNumbers[0],
        FamilyName: inputParticipantRecord[0].family_name,
        DateOfBirth: inputParticipantRecord[0].date_of_birth
      };
    
      const response = await BlockParticipant(request, blockPayload);
    })

    // Assert that the participant's blocked flag is set to 1 in participant management table.
    await test.step('The participant received from the api should have the blocked flag set as 1', async () => {
      const response = await getRecordsFromParticipantManagementService(request);
      expect(response.data[0].BlockedFlag).toBe(1);
    })

  });
})



// test.describe.serial(' @api Negative - Block participant called', async () => {

//   test('@DTOSS-5941-01 204 - @TC14_SIT Verify that an error message is displayed when BS Select attempts to retrieve an already retrieved cohort(ADD)', async ({ request }, testInfo) => {


//     const [checkInDatabase, inputParticipantRecord, nhsNumbers, testFilesPath] = await getApiTestData(testInfo.title);

//     await cleanupDatabaseFromAPI(request, nhsNumbers);


//     const parquetFile = await createParquetFromJson(nhsNumbers, inputParticipantRecord, testFilesPath);

//     await test.step(`When 10 ADD participants are processed via storage`, async () => {
//       await processFileViaStorage(parquetFile);
//     });



//     await test.step(`Then participants should be updated in the cohort ready to be picked up`, async () => {
//       await validateSqlDatabaseFromAPI(request, checkInDatabase);
//     });


//     await test.step(`And 204 status code should be received`, async () => {

//       const requestIdNotExists = '81b723eb-8b40-46bc-84dd-2459c22d69be';

//       const response = await getRecordsFromBsSelectRetrieveCohort(request, { requestId: requestIdNotExists });

//       const genericValidations = composeValidators(
//         expectStatus(204),
//         validateResponseByStatus()
//       );
//       await genericValidations(response);

//     });
//   });

// });
