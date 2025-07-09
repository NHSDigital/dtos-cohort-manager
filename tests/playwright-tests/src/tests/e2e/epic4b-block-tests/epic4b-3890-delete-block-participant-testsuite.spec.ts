import { test, expect, APIRequestContext } from '@playwright/test';
import { createParquetFromJson } from '../../../../parquet/parquet-multiplier';
import { getApiTestData, processFileViaStorage, cleanupDatabaseFromAPI, validateSqlDatabaseFromAPI } from '../../../steps/steps';
import { composeValidators, expectStatus, validateResponseByStatus } from '../../../../api/responseValidators';
import { deleteParticipant } from '../../../../api/distributionService/bsSelectService';
import { getRecordsFromCohortDistributionService } from '../../../../api/dataService/cohortDistributionService';

test.describe.serial('@regression @e2e @epic4b Delete-Block-Participant - CohortDistribution Validation', () => {
  test('@DTOSS-7689-01 - Verify participant is deleted from CohortDistributionDataService', async ({ request }, testInfo) => {
    const [validations, inputParticipantRecord, nhsNumbers, testFilesPath] = await getApiTestData(testInfo.title, 'ADD');

    await cleanupDatabaseFromAPI(request, nhsNumbers);

    const parquetFile = await createParquetFromJson(nhsNumbers, inputParticipantRecord, testFilesPath);

    await test.step(`When participant is inserted via storage`, async () => {
      await processFileViaStorage(parquetFile);
    });

    await test.step(`Wait for participant insertion to complete`, async () => {
      await validateSqlDatabaseFromAPI(request, validations);
    });

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
});

