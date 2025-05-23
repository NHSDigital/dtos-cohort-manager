import { getRecordsFromExceptionService } from '../../../api/dataService/exceptionService';
import { getRecordsFromBsSelectRetrieveAudit, getRecordsFromBsSelectRetrieveCohort } from '../../../api/distributionService/bsSelectService';
import { composeValidators, expectStatus, validateResponseByStatus } from '../../../api/responseValidators';
import { expect, test, testWithAmended, testWithTwoAmendments } from '../../fixtures/test-fixtures';
import { TestHooks } from '../../hooks/test-hooks';
import { processFileViaStorage, validateSqlDatabaseFromAPI } from "../../steps/steps";
import { getRecordsFromCohortDistributionService } from '../../../api/dataService/cohortDistributionService';
import { calledEndpointsOrder, fetchApiResponse, setLogger, validateApiResponse } from '../../../api/apiHelper';
import { checkBlobExists, waitForBlob } from '../../../storage/azureStorage';
import path from 'path';
import { BlobServiceClient } from '@azure/storage-blob';
import { config } from '../../../config/env'

test.describe('@regression @e2e @epic3-high-priority Tests', () => {

  TestHooks.setupAllTestHooks();

  test('@DTOSS-6326-01 - Transformation - Invalid Flag triggers Reason for Removal logic - should apply correct transformations when invalidFlag is true', {
    annotation: {
      type: 'Requirement',
      description: 'Tests - https://nhsd-jira.digital.nhs.uk/browse/DTOSS-5396',
    },
  }, async ({ request, testData }) => {

    await test.step('Then Reason for Removal is set to ORR, Primary Care Provider to null, and  Reason for Removal Date to todays date', async () => {
      let checkInDatabaseRunTime = testData.checkInDatabase;
      checkInDatabaseRunTime = checkInDatabaseRunTime.map((record: any) => {
        if (record.validations.ReasonForRemovalDate) {
          record.validations.ReasonForRemovalDate = new Date().toISOString().split("T")[0] + "T00:00:00";
        }
        return record;
      });
      await validateSqlDatabaseFromAPI(request, checkInDatabaseRunTime);
    });
  });

  testWithAmended('@DTOSS-5596-01 - Transformation - does not trigger removal logic when Reason for Removal is NOT - RDR, RDI, RPR', {
    annotation: {
      type: 'Requirement',
      description: 'Tests - https://nhsd-jira.digital.nhs.uk/browse/DTOSS-4771',
    },
  }, async ({ request, testData }) => {

    await test.step('And ADD participants are processed successfully', async () => {
      await validateSqlDatabaseFromAPI(request, testData.checkInDatabaseAdd);
    });

    await test.step('And there should be transformation exceptions rule trigger for ADD participant', async () => {
      const records = await getRecordsFromExceptionService(request);

      const genericValidations = composeValidators(
        expectStatus(204),
        validateResponseByStatus()
      );
      await genericValidations(records);

    });
    await test.step('Then removal logic should not be triggered, and Reason for Removal should be null', async () => {
      await processFileViaStorage(testData.runTimeParquetFileAmend);
      await validateSqlDatabaseFromAPI(request, testData.checkInDatabaseAmend);
    });

    await test.step('And there should be transformation exceptions rule trigger for AMENDED participant', async () => {
      const records = await getRecordsFromExceptionService(request);

      const genericValidations = composeValidators(
        expectStatus(204),
        validateResponseByStatus()
      );
      await genericValidations(records);

    });
  });

  testWithAmended('@DTOSS-5801-01 @Implement Validate Amend fields reason for removal as DEA and date of death empty', async ({ request, testData }) => {


    await test.step(`When ADD participant is processed via storage`, async () => {
      await processFileViaStorage(testData.runTimeParquetFileAdd);
    });

    await test.step(`Then ADD record should be updated in the cohort`, async () => {
      await validateSqlDatabaseFromAPI(request, testData.checkInDatabaseAdd);
    });

    await test.step(`When same ADD participant record is AMENDED via storage for ${testData.nhsNumberAmend}`, async () => {
      await processFileViaStorage(testData.runTimeParquetFileAmend);
    });

    await test.step(`Then the record should end up in exception management`, async () => {
      await validateSqlDatabaseFromAPI(request, testData.checkInDatabaseAmend);
    });
  });

  testWithAmended('@DTOSS-5589-01 @Implement Validate Amend fields reason for removal as null and date of death present', async ({ request, testData }) => {


    await test.step(`When ADD participant is processed via storage`, async () => {
      await processFileViaStorage(testData.runTimeParquetFileAdd);
    });

    await test.step(`Then ADD record should be updated in the cohort`, async () => {
      await validateSqlDatabaseFromAPI(request, testData.checkInDatabaseAdd);
    });

    await test.step(`When same ADD participant record is AMENDED via storage for ${testData.nhsNumberAmend}`, async () => {
      await processFileViaStorage(testData.runTimeParquetFileAmend);
    });

    await test.step(`Then the record should end up in exception management`, async () => {
      await validateSqlDatabaseFromAPI(request, testData.checkInDatabaseAmend);
    });
  });

  testWithAmended('@DTOSS-5407-01 @Implement Validate Amend fields reason for removal as invalid "RFR" and date of death present', async ({ request, testData }) => {


    await test.step(`When ADD participant is processed via storage`, async () => {
      await processFileViaStorage(testData.runTimeParquetFileAdd);
    });

    await test.step(`Then ADD record should be updated in the cohort`, async () => {
      await validateSqlDatabaseFromAPI(request, testData.checkInDatabaseAdd);
    });

    await test.step(`When same ADD participant record is AMENDED via storage for ${testData.nhsNumberAmend}`, async () => {
      await processFileViaStorage(testData.runTimeParquetFileAmend);
    });

    await test.step(`Then the record should end up in exception management`, async () => {
      await validateSqlDatabaseFromAPI(request, testData.checkInDatabaseAmend);
    });
  });

  testWithAmended('@DTOSS-5406-01 @Implement Validate Amend fields reason for removal as invalid "RDI" and date of death present', async ({ request, testData }) => {
    await test.step(`When ADD participant is processed via storage`, async () => {
      await processFileViaStorage(testData.runTimeParquetFileAdd);
    });

    await test.step(`Then ADD record should be updated in the cohort`, async () => {
      await validateSqlDatabaseFromAPI(request, testData.checkInDatabaseAdd);
    });

    await test.step(`When same ADD participant record is AMENDED via storage for ${testData.nhsNumberAmend}`, async () => {
      await processFileViaStorage(testData.runTimeParquetFileAmend);
    });

    await test.step(`Then the record should end up in exception management`, async () => {
      await validateSqlDatabaseFromAPI(request, testData.checkInDatabaseAmend);
    });
  });

  testWithAmended('@DTOSS-5405-01 @Implement Validate Amend fields reason for removal as invalid "RPI" and date of death present', async ({ request, testData }) => {
    await test.step(`When ADD participant is processed via storage`, async () => {
      await processFileViaStorage(testData.runTimeParquetFileAdd);
    });

    await test.step(`Then ADD record should be updated in the cohort`, async () => {
      await validateSqlDatabaseFromAPI(request, testData.checkInDatabaseAdd);
    });

    await test.step(`When same ADD participant record is AMENDED via storage for ${testData.nhsNumberAmend}`, async () => {
      await processFileViaStorage(testData.runTimeParquetFileAmend);
    });

    await test.step(`Then the record should end up in exception management`, async () => {
      await validateSqlDatabaseFromAPI(request, testData.checkInDatabaseAmend);
    });
  });

  testWithAmended('@DTOSS-5565-01 @P1 Validation - Not possible to determine BSO dummy practice code for subject',{
    annotation: {
      type: 'Requirement',
      description: 'Tests - https://nhsd-jira.digital.nhs.uk/browse/DTOSS-4283',
    },
  }, async ({ request, testData }) => {
    await test.step(`When ADD participant is processed via storage`, async () => {
      await processFileViaStorage(testData.runTimeParquetFileAdd);
    });

    await test.step(`Then ADD record should be updated in the cohort`, async () => {
      await validateSqlDatabaseFromAPI(request, testData.checkInDatabaseAdd);
    });

    await test.step(`When same ADD participant record is AMENDED via storage for ${testData.nhsNumberAmend}`, async () => {
      await processFileViaStorage(testData.runTimeParquetFileAmend);
    });

    await test.step(`Then the record should end up in exception management`, async () => {
      await validateSqlDatabaseFromAPI(request, testData.checkInDatabaseAmend);
    });
  });

  testWithAmended('@DTOSS-5566-01 @P1 Validation - Not possible to determine BSO dummy practice code for subject - scenario 2',{
    annotation: {
      type: 'Requirement',
      description: 'Tests - https://nhsd-jira.digital.nhs.uk/browse/DTOSS-4283',
    },
  }, async ({ request, testData }) => {
    await test.step(`When ADD participant is processed via storage`, async () => {
      await processFileViaStorage(testData.runTimeParquetFileAdd);
    });

    await test.step(`Then ADD record should be updated in the cohort`, async () => {
      await validateSqlDatabaseFromAPI(request, testData.checkInDatabaseAdd);
    });

    await test.step(`When same ADD participant record is AMENDED via storage for ${testData.nhsNumberAmend}`, async () => {
      await processFileViaStorage(testData.runTimeParquetFileAmend);
    });

    await test.step(`Then the record should end up in exception management`, async () => {
      await validateSqlDatabaseFromAPI(request, testData.checkInDatabaseAmend);
    });
  });

  testWithTwoAmendments('@DTOSS-5568-01 @P1 Validation - Cohort Distribution_Raise manual exception when list of conditions are true for a record(AMENDED Twice)',{
    annotation: {
      type: 'Requirement',
      description: 'Tests - https://nhsd-jira.digital.nhs.uk/browse/DTOSS-4283',
    },
  }, async ({ request, testData }) => {
    await test.step(`When ADD participant is processed via storage`, async () => {
      await processFileViaStorage(testData.runTimeParquetFileAdd);
    });

    await test.step(`Then ADD record should be updated in the cohort`, async () => {
      await validateSqlDatabaseFromAPI(request, testData.checkInDatabaseAdd);
    });

    await test.step(`When same ADD participant record is AMENDED via storage for ${testData.nhsNumberAmend}`, async () => {
      await processFileViaStorage(testData.runTimeParquetFileAmend);
    });

    await test.step(`Then the record should end up in exception management`, async () => {
      await validateSqlDatabaseFromAPI(request, testData.checkInDatabaseAmend);
    });

    await test.step(`When same ADD participant record is AMENDED via storage for ${testData.nhsNumberAmend}`, async () => {
      await processFileViaStorage(testData.runTimeParquetFileSecondAmend);
    });

    await test.step(`Then the record should end up in exception management`, async () => {
      await validateSqlDatabaseFromAPI(request, testData.checkInDatabaseSecondAmend);
    });

  });

  testWithTwoAmendments('@DTOSS-5569-01 @P1 Validation - Cohort Distribution_Raise manual exception when list of conditions are true for a record(AMENDED Twice)',{
    annotation: {
      type: 'Requirement',
      description: 'Tests - https://nhsd-jira.digital.nhs.uk/browse/DTOSS-4283',
    },
  }, async ({ request, testData }) => {
    await test.step(`When ADD participant is processed via storage`, async () => {
      await processFileViaStorage(testData.runTimeParquetFileAdd);
    });

    await test.step(`Then ADD record should be updated in the cohort`, async () => {
      await validateSqlDatabaseFromAPI(request, testData.checkInDatabaseAdd);
    });

    await test.step(`When same ADD participant record is AMENDED via storage for ${testData.nhsNumberAmend}`, async () => {
      await processFileViaStorage(testData.runTimeParquetFileAmend);
    });

    await test.step(`Then the record should end up in exception management`, async () => {
      await validateSqlDatabaseFromAPI(request, testData.checkInDatabaseAmend);
    });

    await test.step(`When same ADD participant record is AMENDED via storage for ${testData.nhsNumberAmend}`, async () => {
      await processFileViaStorage(testData.runTimeParquetFileSecondAmend);
    });

    await test.step(`Then the record should end up in exception management`, async () => {
      await validateSqlDatabaseFromAPI(request, testData.checkInDatabaseSecondAmend);
    });

  });

  testWithTwoAmendments('@DTOSS-5570-01 @P1 Validation - Cohort Distribution_Raise manual exception when list of conditions are true for a record(AMENDED Twice)',{
    annotation: {
      type: 'Requirement',
      description: 'Tests - https://nhsd-jira.digital.nhs.uk/browse/DTOSS-4283',
    },
  }, async ({ request, testData }) => {
    await test.step(`When ADD participant is processed via storage`, async () => {
      await processFileViaStorage(testData.runTimeParquetFileAdd);
    });

    await test.step(`Then ADD record should be updated in the cohort`, async () => {
      await validateSqlDatabaseFromAPI(request, testData.checkInDatabaseAdd);
    });

    await test.step(`When same ADD participant record is AMENDED via storage for ${testData.nhsNumberAmend}`, async () => {
      await processFileViaStorage(testData.runTimeParquetFileAmend);
    });

    await test.step(`Then the record should end up in exception management`, async () => {
      await validateSqlDatabaseFromAPI(request, testData.checkInDatabaseAmend);
    });

    await test.step(`When same ADD participant record is AMENDED via storage for ${testData.nhsNumberAmend}`, async () => {
      await processFileViaStorage(testData.runTimeParquetFileSecondAmend);
    });

    await test.step(`Then the record should end up in exception management`, async () => {
      await validateSqlDatabaseFromAPI(request, testData.checkInDatabaseSecondAmend);
    });

  });

  test('@DTOSS-5560-01 - BS Select - Records are received where IsExtracted is set to 0', {
    annotation: {
      type: 'Requirement',
      description: 'Tests - https://nhsd-jira.digital.nhs.uk/browse/DTOSS-3650',
    },
  }, async ({ request, testData }) => {

    await test.step('Then processed ADD participant should be received using bs select get request where IsExtracted = 0', async () => {
      await validateSqlDatabaseFromAPI(request, testData.checkInDatabase);
      const ExpectedRowCount = 1;

      const response = await getRecordsFromBsSelectRetrieveCohort(request, { rowCount: 10, screeningServiceId: 1 });
      expect(response.data.length).toBe(ExpectedRowCount);

    });

    await test.step('And IsExtracted flag is set to 1', async () => {
      const response = await getRecordsFromCohortDistributionService(request);
      const firstRecord = response.data.find(() => true);
      expect(firstRecord?.IsExtracted).toBe(1);
    });
  });

  test('@DTOSS-5584-01 - BS Select - 204 if IsExtracted is set to 1', {
    annotation: {
      type: 'Requirement',
      description: 'Tests - https://nhsd-jira.digital.nhs.uk/browse/DTOSS-3714',
    },
  }, async ({ request, testData }) => {

    await test.step('Then processed ADD participant should be received using bs select get request where IsExtracted = 0', async () => {
      await validateSqlDatabaseFromAPI(request, testData.checkInDatabase);

      const response = await getRecordsFromBsSelectRetrieveCohort(request, { rowCount: 10, screeningServiceId: 1 });
      expect(response.data.length).toBe(1);

    });

    await test.step('And IsExtracted flag is set to 1', async () => {
      const response = await getRecordsFromCohortDistributionService(request);
      const firstRecord = response.data.find(() => true);
      expect(firstRecord?.IsExtracted).toBe(1);
    });

    await test.step('When records are received again using bs select API where IsExtracted = 1, Then API should return no records with status 204', async () => {

      const response = await getRecordsFromBsSelectRetrieveCohort(request, { rowCount: 10, screeningServiceId: 1 });
      const genericValidations = composeValidators(
        expectStatus(204),
        validateResponseByStatus()
      );
      await genericValidations(response);
    });


  });

  test('@DTOSS-5563-01 - Empty RowCount should log 204 in BS_SELECT_REQUEST_AUDIT table ', {
    annotation: [{
      type: 'Requirement',
      description: 'Tests - https://nhsd-jira.digital.nhs.uk/browse/DTOSS-5563',
    }, {
      type: 'Defect',
      description: 'Tests - https://nhsd-jira.digital.nhs.uk/browse/DTOSS-6118',
    },]
  }, async ({ request, testData }) => {

    await test.step('And ADD participant is processed with IsExtracted = 0', async () => {
      await validateSqlDatabaseFromAPI(request, testData.checkInDatabase);

    });

    await test.step('When Retrieve Cohort BS Select API returns no records with status 204, with RowCount as empty', async () => {
      const response = await getRecordsFromBsSelectRetrieveCohort(request, { rowCount: ``, screeningServiceId: 1 });
      const genericValidations = composeValidators(
        expectStatus(204),
        validateResponseByStatus()
      );
      await genericValidations(response);
    });

    await test.step('Then BS_SELECT_REQUEST_AUDIT should have an entry for 204', async () => {
      const response = await getRecordsFromBsSelectRetrieveAudit(request)
      const firstRecord = response.data.find(() => true);
      expect(firstRecord?.StatusCode).toBe("204");
    });

  });

  test.only('@DTOSS-5348-01 @AddParticipant Verify all Functions Called', {
    annotation: {
      type: 'Requirement',
      description: 'Tests - https://nhsd-jira.digital.nhs.uk/browse/DTOSS-5348',
    },
  }, async ({ request, testData }) => {

    await test.step('ReceiveCaasFile processes the uploaded participant data file', async () => {

      await processFileViaStorage(testData.runTimeParquetFile);

    });

    await test.step('Verify ProcessCaasFile data file', async () => {

      const expectedBlobName = path.basename(testData.runTimeParquetFile);
      const outputFileExists = await checkBlobExists(expectedBlobName);

      expect(outputFileExists).toBe(true);
    });

    await test.step('Then participant record is added to cohort distribution table', async () => {
      await validateSqlDatabaseFromAPI(request, testData.checkInDatabase);
    });
  });
});
