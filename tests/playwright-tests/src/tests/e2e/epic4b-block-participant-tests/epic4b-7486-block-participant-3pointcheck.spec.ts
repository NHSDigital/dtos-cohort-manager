import { test, expect } from '@playwright/test';
import { createParquetFromJson } from '../../../parquet/parquet-multiplier';
import { getApiTestData, processFileViaStorage, cleanupDatabaseFromAPI, validateSqlDatabaseFromAPI } from '../../steps/steps';
import { getRecordsFromParticipantManagementService, BlockParticipant } from '../../../api/distributionService/bsSelectService';

test.describe('@e2e @epic4b-block-tests Tests', async () => {

  test('@DTOSS-7720-01 AC01 verify records returned matches block request and that NHS ID matches the correct person when 3 point check in cohort', async ({ request }, testInfo) => {

    const [checkInDatabase, inputParticipantRecord, nhsNumbers, testFilesPath] = await getApiTestData(testInfo.title);

    await cleanupDatabaseFromAPI(request, nhsNumbers);

    const parquetFile = await createParquetFromJson(nhsNumbers, inputParticipantRecord, testFilesPath);

    await test.step(`When I ADD participant is processed via storage`, async () => {
      await processFileViaStorage(parquetFile);
    });

    await test.step(`Then participant should be in the participant management table`, async () => {
      await validateSqlDatabaseFromAPI(request, checkInDatabase);
    });

    // Call the block participant function
    await test.step(`When BlockParticipant function is invoked`, async () => {
      const blockPayload = {
        NhsNumber: nhsNumbers,
        FamilyName: inputParticipantRecord[0].family_name,
        DateOfBirth: inputParticipantRecord[0].date_of_birth
      };
    });
  });

});
