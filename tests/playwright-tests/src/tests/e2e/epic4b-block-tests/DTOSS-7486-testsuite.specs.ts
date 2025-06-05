import { test, expect } from '@playwright/test';
import { createParquetFromJson } from '../../../../parquet/parquet-multiplier';
import { getApiTestData, processFileViaStorage, cleanupDatabaseFromAPI, validateSqlDatabaseFromAPI } from '../../../steps/steps';
import { BlockParticipant, getRecordsFromBsSelectRetrieveCohort } from '../../../../api/distributionService/bsSelectService'
import { getRecordsFromParticipantManagementService } from '../../../../api/distributionService/participantService';

test.describe.serial('3 point check Block Participant', async () => {

  test.only('@DTOSS-7720-01 @smoke @api - @TC1 Verify the see to block a participant', async ({ request }, testInfo) => {

    const [checkInDatabase, inputParticipantRecord, nhsNumbers, testFilesPath] = await getApiTestData(testInfo.title);

    await cleanupDatabaseFromAPI(request, nhsNumbers);

    const parquetFile = await createParquetFromJson(nhsNumbers, inputParticipantRecord, testFilesPath);

    await test.step(` ADD participant is processed via storage`, async () => {
      await processFileViaStorage(parquetFile);
    });

    await test.step(`Given I have a participant's details from the block request`, async () => {
      await validateSqlDatabaseFromAPI(request, testData.checkInDatabase);
    });

    await test.step(`When BlockParticipant function is invoked`, async () => {
      const blockPayload = {
          NhsNumber: nhsNumbers[0],
          FamilyName: inputParticipantRecord[0].family_name,
          DateOfBirth: `${inputParticipantRecord[0].date_of_birth}`
      };

    await test.step('Then I should be able to see records returned that match the details in the block request to be able to verify that the NHS ID matches the correct person`, async () => {
       const response = await getRecordsFromParticipantManagementService(request);

      //Extend custom assertions
      expect(response.data[0].BlockedFlag).toBe(1);
    })
    });
    });

  });
