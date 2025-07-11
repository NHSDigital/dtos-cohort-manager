import { test, expect, APIRequestContext } from '@playwright/test';
import { createParquetFromJson } from '../../../parquet/parquet-multiplier';
import { getApiTestData, processFileViaStorage, cleanupDatabaseFromAPI, validateSqlDatabaseFromAPI } from '../../steps/steps';
import { composeValidators, expectStatus, validateResponseByStatus } from '../../../api/responseValidators';
import { BlockParticipant, deleteParticipant, getRecordsFromParticipantManagementService } from '../../../api/distributionService/bsSelectService';
import { getRecordsFromCohortDistributionService } from '../../../api/dataService/cohortDistributionService';

test.describe.serial('@regression @e2e @epic4b-block-tests Delete-Block-Participant - CohortDistribution Validation', () => {
  test('@DTOSS-7689-01 - AC1 - Verify participant is deleted from CohortDistributionDataService', async ({ request }, testInfo) => {
    const [validations, inputParticipantRecord, nhsNumbers, testFilesPath] = await getApiTestData(testInfo.title, 'ADD');

    await cleanupDatabaseFromAPI(request, nhsNumbers);

    const parquetFile = await createParquetFromJson(nhsNumbers, inputParticipantRecord, testFilesPath);

    await test.step(`When participant is inserted via storage`, async () => {
      await processFileViaStorage(parquetFile);
    });

    await test.step(`Given for participant insertion to Cohort`, async () => {
      await validateSqlDatabaseFromAPI(request, validations);
    });

    // Call the block participant function

      const payload = {
        NhsNumber: nhsNumbers[0],
        FamilyName: inputParticipantRecord[0].family_name,
        DateOfBirth: inputParticipantRecord[0].date_of_birth
      };

    await test.step(`When BlockParticipant function is invoked`, async () => {
          await BlockParticipant(request, payload);
         })

        // Assert that the participant's blocked flag is set to 1 in participant management table.
    await test.step('The participant received from the api should have the blocked flag set as 1', async () => {
          const response = await getRecordsFromParticipantManagementService(request);
          expect(response.data[0].BlockedFlag).toBe(1);
        })

   // Call the delete participant function
    await test.step(`When DeleteParticipant function is invoked`, async () => {
      const deletePayload = {
        NhsNumber: nhsNumbers[0],
        FamilyName: inputParticipantRecord[0].family_name,
        DateOfBirth: `${inputParticipantRecord[0].date_of_birth.slice(0, 4)}-${inputParticipantRecord[0].date_of_birth.slice(4, 6)}-${inputParticipantRecord[0].date_of_birth.slice(6, 8)}`
      };

      const response = await deleteParticipant(request, deletePayload);

      const validators = composeValidators(
        expectStatus(200)
      );
      await validators(response);

      const lastResponse = await getRecordsFromCohortDistributionService(request);
      expect(lastResponse.status).toBe(204);
    });
  });

  test('@DTOSS-7690-01 - AC2 - Verify participant is deleted from CohortDistributionDataService', async ({ request }, testInfo) => {
    const [validations, inputParticipantRecord, nhsNumbers, testFilesPath] = await getApiTestData(testInfo.title, 'ADD');

    await cleanupDatabaseFromAPI(request, nhsNumbers);

    const parquetFile = await createParquetFromJson(nhsNumbers, inputParticipantRecord, testFilesPath);

    await test.step(`When participant is inserted via storage`, async () => {
      await processFileViaStorage(parquetFile);
    });

    await test.step(`Given for participant insertion to Cohort`, async () => {
      await validateSqlDatabaseFromAPI(request, validations);
    });

    // Call the block participant function

    const payload = {
      NhsNumber: nhsNumbers[0],
      FamilyName: inputParticipantRecord[0].family_name,
      DateOfBirth: inputParticipantRecord[0].date_of_birth
    };

    await test.step(`When BlockParticipant function is invoked`, async () => {
      await BlockParticipant(request, payload);
    })

    // Assert that the participant's blocked flag is set to 1 in participant management table.
    await test.step('The participant received from the api should have the blocked flag set as 1', async () => {
      const response = await getRecordsFromParticipantManagementService(request);
      expect(response.data[0].BlockedFlag).toBe(1);
    })

    // Call the delete participant function
    await test.step(`When DeleteParticipant function is invoked`, async () => {
      const deletePayload = {
        NhsNumber: nhsNumbers[0],
        FamilyName: 'InCorrectFamilyName',
        DateOfBirth: `${inputParticipantRecord[0].date_of_birth.slice(0, 4)}-${inputParticipantRecord[0].date_of_birth.slice(4, 6)}-${inputParticipantRecord[0].date_of_birth.slice(6, 8)}`
      };

      //Call Delete participant with incorrect family name and rest all values as correct
      // This should return 404
      const response = await deleteParticipant(request, deletePayload);
      const validators = composeValidators(
        expectStatus(404)
      );
      await validators(response);
      const lastResponse = await getRecordsFromCohortDistributionService(request);
      expect(lastResponse.status).toBe(200);

    });
  });
});

