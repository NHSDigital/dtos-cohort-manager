import { expect } from '@playwright/test';
import type { TestInfo } from '@playwright/test';
import type { APIRequestContext } from '@playwright/test';
import { test, testWithAmended } from '../../fixtures/test-fixtures';
import { createParquetFromJson } from '../../../parquet/parquet-multiplier';
import { getApiTestData, processFileViaStorage, validateSqlDatabaseFromAPI, cleanupDatabaseFromAPI } from '../../steps/steps';
import { BlockParticipant, deleteParticipant, getRecordsFromParticipantManagementService } from '../../../api/distributionService/bsSelectService';
import { expectStatus, composeValidators } from '../../../api/responseValidators';
import { TestHooks } from '../../hooks/test-hooks';
import { getRecordsFromCohortDistributionService } from '../../../api/dataService/cohortDistributionService';

test.describe('@regression @e2e @epic4b-block-tests Tests', async () => {
   TestHooks.setupAllTestHooks();

  test('@DTOSS-7610-01 AC01 Verify block a participant not processed to COHORT - ADD', async ({ request }, testInfo) => {
    const [checkInDatabase, inputParticipantRecord, nhsNumbers, testFilesPath] = await getApiTestData(testInfo.title);

    await cleanupDatabaseFromAPI(request, nhsNumbers);

    // Seed the participant with BlockedFlag: 1 before ADD
    await test.step(`Seed participant with BlockedFlag: 1 before ADD`, async () => {
      // Insert participant with BlockedFlag: 1
      const seedPayload = {
        ...inputParticipantRecord[0],
        BlockedFlag: 1
      };
      // Use the API to insert the participant (simulate DB seed)
      await processFileViaStorage(await createParquetFromJson(nhsNumbers, [seedPayload], testFilesPath));
    });

    // Create and process the ADD parquet file (should not process to cohort)
    const parquetFile = await createParquetFromJson(nhsNumbers, inputParticipantRecord, testFilesPath);
    await test.step(`When ADD participant is processed via storage`, async () => {
      await processFileViaStorage(parquetFile);
    });

    // Verify the participant is in participant management table with blocked flag
    await test.step(`Then participant should be in participant management table with blocked flag`, async () => {
      const response = await getRecordsFromParticipantManagementService(request);
      expect(response.data[0].BlockedFlag).toBe(1);
    });

    // Verify that the participant's data is not in cohort distribution
    await test.step(`Then participant should not be present in cohort distribution`, async () => {
      const response = await getRecordsFromCohortDistributionService(request);
      expect(response.status).toBe(204); // No content, indicating no records found
    });

    // Verify the exception was created
    await test.step(`Then exception should be in the exception management table`, async () => {
      await validateSqlDatabaseFromAPI(request, checkInDatabase);
    });
  });

  testWithAmended('@DTOSS-7614-01 AC01 Verify block a participant not processed to COHORT - Amend', async ({ request, testData }: { request: APIRequestContext; testData: any }, testInfo: TestInfo) => {
    // Seed the participant with BlockedFlag: 1 before AMEND
    await test.step(`Seed participant with BlockedFlag: 1 before AMEND`, async () => {
      const seedPayload = {
        ...testData.inputParticipantRecord[0],
        BlockedFlag: 1
      };
      await processFileViaStorage(await createParquetFromJson(testData.nhsNumbers, [seedPayload], testData.testFilesPath));
    });

    // Try to amend the blocked participant
    await test.step(`When Amend participant is processed via storage`, async () => {
      await processFileViaStorage(testData.runTimeParquetFileAmend);
    });

    // Verify the amend was not processed and exception was created
    await test.step(`Then exception should be in the exception management table`, async () => {
      await validateSqlDatabaseFromAPI(request, testData.checkInDatabaseAmend);
    });

    // Verify amendment was not passed to cohort distribution
    await test.step(`Then amended data should not be present in cohort distribution`, async () => {
      const response = await getRecordsFromCohortDistributionService(request);
      expect(response.status).toBe(204); // No content, indicating no records found
    });
   });
test('@DTOSS-7615-01 - AC1 - Verify blocked participant deletion is not processed to cohort distribution', async ({ request }, testInfo) => {
    const [validations, inputParticipantRecord, nhsNumbers, testFilesPath] = await getApiTestData(testInfo.title, 'DELETE');

    await cleanupDatabaseFromAPI(request, nhsNumbers);

    // Seed the participant with BlockedFlag: 1 before DELETE
    await test.step(`Seed participant with BlockedFlag: 1 before DELETE`, async () => {
      const seedPayload = {
        ...inputParticipantRecord[0],
        BlockedFlag: 1
      };
      await processFileViaStorage(await createParquetFromJson(nhsNumbers, [seedPayload], testFilesPath));
    });

    // Attempt to delete the blocked participant
    await test.step(`When DeleteParticipant function is invoked for blocked participant`, async () => {
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
    });

    // Verify the deletion was not processed to cohort distribution
    await test.step('Then participant data should not exist in cohort distribution', async () => {
      const response = await getRecordsFromCohortDistributionService(request);
      expect(response.status).toBe(204); // No content, indicating no records found
    });

    // Verify exception was created
    await test.step('Then exception should be created for blocked deletion', async () => {
      await validateSqlDatabaseFromAPI(request, validations);
    });
  });

  test('@DTOSS-7616-01 AC02 Verify no NBO exception raised for blocked participant - ADD', async ({ request }, testInfo) => {
    const [checkInDatabase, inputParticipantRecord, nhsNumbers, testFilesPath] = await getApiTestData(testInfo.title);

    await cleanupDatabaseFromAPI(request, nhsNumbers);

    // Seed the participant with BlockedFlag: 1 before ADD
    await test.step(`Seed participant with BlockedFlag: 1 before ADD`, async () => {
      const seedPayload = {
        ...inputParticipantRecord[0],
        BlockedFlag: 1
      };
      await processFileViaStorage(await createParquetFromJson(nhsNumbers, [seedPayload], testFilesPath));
    });

    // Create and process the ADD parquet file
    const parquetFile = await createParquetFromJson(nhsNumbers, inputParticipantRecord, testFilesPath);
    await test.step(`When ADD participant is processed via storage`, async () => {
      await processFileViaStorage(parquetFile);
    });

    // Verify the validation exception in the management table is not an NBO type
    await test.step(`Then validation exception should be created but not as NBO type`, async () => {
      await validateSqlDatabaseFromAPI(request, {
        apiEndpoint: 'api/ExceptionManagementDataService',
        nhsNumber: nhsNumbers[0],
        validations: {
          nboExceptionCount: 0,  // Verify no NBO exceptions exist
          Category: 'CaaS',     // Verify exception is CaaS type
          RuleDescription: 'Unable to process participant record. Participant is marked as blocked.'
        }
      });
    });

    // Double check participant management still shows blocked
    await test.step(`Then participant should remain blocked`, async () => {
      const response = await getRecordsFromParticipantManagementService(request);
      expect(response.data[0].BlockedFlag).toBe(1);
    });
  });

  test('@DTOSS-7660-01 AC02 Verify no NBO exception raised for blocked participant - Amend', async ({ request, testData }: { request: APIRequestContext; testData: any }, testInfo: TestInfo) => {
    await cleanupDatabaseFromAPI(request, testData.nhsNumbers);

    // Seed the participant with BlockedFlag: 1 before AMEND
    await test.step(`Seed participant with BlockedFlag: 1 before AMEND`, async () => {
      const seedPayload = {
        ...testData.inputParticipantRecord[0],
        BlockedFlag: 1
      };
      await processFileViaStorage(await createParquetFromJson(testData.nhsNumbers, [seedPayload], testData.testFilesPath));
    });

    // Generate AMEND parquet file from JSON before processing
    const parquetFileAmend = await createParquetFromJson(
      testData.nhsNumbers,
      testData.inputParticipantRecord,
      testData.testFilesPath,
      'AMENDED'
    );

    // Attempt to amend the blocked participant
    await test.step(`When Amend participant is processed via storage`, async () => {
      await processFileViaStorage(parquetFileAmend);
    });

    // Verify regular validation exception exists (non-NBO) for the amendment
    await test.step(`Then validation exception should be created but not as NBO type`, async () => {
      await validateSqlDatabaseFromAPI(request, {
        apiEndpoint: 'api/ExceptionManagementDataService',
        nhsNumber: testData.nhsNumbers[0],
        validations: {
          nboExceptionCount: 0,  // Verify no NBO exceptions exist
          Category: 'CaaS',     // Verify exception is CaaS type
          RuleDescription: 'Unable to process participant record. Participant is marked as blocked.'
        }
      });
    });

    // Double check participant management still shows blocked
    await test.step('Then participant should remain blocked', async () => {
      const response = await getRecordsFromParticipantManagementService(request);
      expect(response.data[0].BlockedFlag).toBe(1);
    });
  });

  test('@DTOSS-7661-01 AC02 Verify no NBO exception raised for blocked participant - Delete', async ({ request }, testInfo) => {
    const [validations, inputParticipantRecord, nhsNumbers, testFilesPath] = await getApiTestData(testInfo.title, 'DELETE');

    await cleanupDatabaseFromAPI(request, nhsNumbers);

    // Seed the participant with BlockedFlag: 1 before DELETE
    await test.step(`Seed participant with BlockedFlag: 1 before DELETE`, async () => {
      const seedPayload = {
        ...inputParticipantRecord[0],
        BlockedFlag: 1
      };
      await processFileViaStorage(await createParquetFromJson(nhsNumbers, [seedPayload], testFilesPath));
    });

    // Create and process the DELETE parquet file
    const parquetFile = await createParquetFromJson(nhsNumbers, inputParticipantRecord, testFilesPath, 'DELETE');
    await test.step(`When DELETE participant is processed via storage`, async () => {
      await processFileViaStorage(parquetFile);
    });

    // Verify the validation exception in the management table is not an NBO type
    await test.step(`Then validation exception should be created but not as NBO type`, async () => {
      await validateSqlDatabaseFromAPI(request, {
        apiEndpoint: 'api/ExceptionManagementDataService',
        nhsNumber: nhsNumbers[0],
        validations: {
          nboExceptionCount: 0,  // Verify no NBO exceptions exist
          Category: 'CaaS',     // Verify exception is CaaS type
          RuleDescription: 'Unable to process participant record. Participant is marked as blocked.'
        }
      });
    });

    // Double check participant management still shows blocked
    await test.step('Then participant should remain blocked', async () => {
      const response = await getRecordsFromParticipantManagementService(request);
      expect(response.data[0].BlockedFlag).toBe(1);
    });
  });

  test('@DTOSS-7663-01 AC03 Verify no NBO exception when blocked ineligible participant becomes eligible - ADD', async ({ request }, testInfo) => {
    const [validations, inputParticipantRecord, nhsNumbers, testFilesPath] = await getApiTestData(testInfo.title, 'ADD');

    await cleanupDatabaseFromAPI(request, nhsNumbers);

    // Seed the participant as blocked and ineligible before ADD
    await test.step(`Seed participant as blocked and ineligible before ADD`, async () => {
      const seedPayload = {
        ...inputParticipantRecord[0],
        BlockedFlag: 1,
        EligibilityFlag: "0" // Set as ineligible
      };
      await processFileViaStorage(await createParquetFromJson(nhsNumbers, [seedPayload], testFilesPath));
    });

    // Now try to add the participant as eligible
    const eligibleParticipant = {
      ...inputParticipantRecord[0],
      BlockedFlag: 1,
      EligibilityFlag: "1" // Set as eligible
    };
    const parquetFileEligible = await createParquetFromJson(nhsNumbers, [eligibleParticipant], testFilesPath);
    await test.step(`When participant becomes eligible via ADD`, async () => {
      await processFileViaStorage(parquetFileEligible);
    });

    // Verify validation exception exists (non-NBO) for the eligibility change attempt
    await test.step(`Then validation exception should be created but not as NBO type`, async () => {
      await validateSqlDatabaseFromAPI(request, {
        apiEndpoint: 'api/ExceptionManagementDataService',
        nhsNumber: nhsNumbers[0],
        validations: {
          nboExceptionCount: 0,  // Verify no NBO exceptions exist
          Category: 'CaaS',     // Verify exception is CaaS type
          RuleDescription: 'Unable to process participant record. Participant is marked as blocked.'
        }
      });
    });

    // Verify participant remains blocked and ineligible
    await test.step('Then participant should remain blocked and ineligible', async () => {
      const response = await getRecordsFromParticipantManagementService(request);
      expect(response.data[0].BlockedFlag).toBe(1);
      expect(response.data[0].EligibilityFlag).toBe("0");
    });

    // Verify not in cohort distribution due to being blocked
    await test.step('Then participant should not be in cohort distribution', async () => {
      const response = await getRecordsFromCohortDistributionService(request);
      expect(response.status).toBe(204);  // No content, as blocked participants shouldn't be in cohort distribution
    });
  });

  test('@DTOSS-7664-01 AC04 Verify audit logs are updated for blocked participant - ADD', async ({ request }, testInfo) => {
    const [checkInDatabase, inputParticipantRecord, nhsNumbers, testFilesPath] = await getApiTestData(testInfo.title);

    await cleanupDatabaseFromAPI(request, nhsNumbers);

    // Seed the participant with BlockedFlag: 1 before ADD
    await test.step(`Seed participant with BlockedFlag: 1 before ADD`, async () => {
      const seedPayload = {
        ...inputParticipantRecord[0],
        BlockedFlag: 1
      };
      await processFileViaStorage(await createParquetFromJson(nhsNumbers, [seedPayload], testFilesPath));
    });

    // Create and process the ADD parquet file
    const parquetFile = await createParquetFromJson(nhsNumbers, inputParticipantRecord, testFilesPath);
    await test.step(`When ADD participant is processed via storage`, async () => {
      await processFileViaStorage(parquetFile);
    });

    // Verify audit logs contain block flag update
    await test.step(`Then audit logs should contain block flag update`, async () => {
      await validateSqlDatabaseFromAPI(request, {
        apiEndpoint: 'BsSelectRequestAudit',
        nhsNumber: nhsNumbers[0],
        validations: {
          BlockedFlag: 1,
          ActionType: 'Block',
          Status: 'Success',
          Details: 'Participant blocked flag updated successfully'
        }
      });
    });

    // Verify audit logs contain ADD action rejection
    await test.step(`Then audit logs should contain ADD action rejection`, async () => {
      await validateSqlDatabaseFromAPI(request, {
        apiEndpoint: 'BsSelectRequestAudit',
        nhsNumber: nhsNumbers[0],
        validations: {
          ActionType: 'Add',
          Status: 'Rejected',
          Details: 'Participant is marked as blocked'
        }
      });
    });

    // Double check participant management still shows blocked
    await test.step(`Then participant should remain blocked`, async () => {
      const response = await getRecordsFromParticipantManagementService(request);
      expect(response.data[0].BlockedFlag).toBe(1);
    });

    // Verify the participant is not in cohort distribution
    await test.step(`Then participant should not be present in cohort distribution`, async () => {
      const response = await getRecordsFromCohortDistributionService(request);
      expect(response.status).toBe(204); // No content, indicating no records found
    });
  });

  testWithAmended('@DTOSS-7665-01 AC04 Verify audit logs are updated for blocked participant - AMEND', async ({ request, testData }: { request: APIRequestContext; testData: any }, testInfo: TestInfo) => {
    await cleanupDatabaseFromAPI(request, testData.nhsNumbers);

    // Seed the participant with BlockedFlag: 1 before AMEND
    await test.step(`Seed participant with BlockedFlag: 1 before AMEND`, async () => {
      const seedPayload = {
        ...testData.inputParticipantRecord[0],
        BlockedFlag: 1
      };
      await processFileViaStorage(await createParquetFromJson(testData.nhsNumbers, [seedPayload], testData.testFilesPath));
    });

    // Create and process the AMEND parquet file
    const parquetFileAmend = await createParquetFromJson(testData.nhsNumbers, testData.inputParticipantRecord, testData.testFilesPath, 'AMENDED');
    await test.step(`When AMEND participant is processed via storage`, async () => {
      await processFileViaStorage(parquetFileAmend);
    });

    // Verify audit logs contain block flag update
    await test.step(`Then audit logs should contain block flag update`, async () => {
      await validateSqlDatabaseFromAPI(request, {
        apiEndpoint: 'BsSelectRequestAudit',
        nhsNumber: testData.nhsNumbers[0],
        validations: {
          BlockedFlag: 1,
          ActionType: 'Block',
          Status: 'Success',
          Details: 'Participant blocked flag updated successfully'
        }
      });
    });

    // Verify audit logs contain AMEND action rejection
    await test.step(`Then audit logs should contain AMEND action rejection`, async () => {
      await validateSqlDatabaseFromAPI(request, {
        apiEndpoint: 'BsSelectRequestAudit',
        nhsNumber: testData.nhsNumbers[0],
        validations: {
          ActionType: 'Amend',
          Status: 'Rejected',
          Details: 'Participant is marked as blocked'
        }
      });
    });

    // Double check participant management still shows blocked
    await test.step('Then participant should remain blocked', async () => {
      const response = await getRecordsFromParticipantManagementService(request);
      expect(response.data[0].BlockedFlag).toBe(1);
    });

    // Verify the amendment did not affect cohort distribution
    await test.step('Then amended data should not be present in cohort distribution', async () => {
      const response = await getRecordsFromCohortDistributionService(request);
      expect(response.status).toBe(204); // No content, indicating no records found
    });
  });

  test('@DTOSS-7666-01 AC04 Verify audit logs are updated for blocked participant - DELETE', async ({ request }, testInfo) => {
    const [validations, inputParticipantRecord, nhsNumbers, testFilesPath] = await getApiTestData(testInfo.title, 'DELETE');

    await cleanupDatabaseFromAPI(request, nhsNumbers);

    // Seed the participant with BlockedFlag: 1 before DELETE
    await test.step(`Seed participant with BlockedFlag: 1 before DELETE`, async () => {
      const seedPayload = {
        ...inputParticipantRecord[0],
        BlockedFlag: 1
      };
      await processFileViaStorage(await createParquetFromJson(nhsNumbers, [seedPayload], testFilesPath));
    });

    // Create and process the DELETE parquet file
    const parquetFile = await createParquetFromJson(nhsNumbers, inputParticipantRecord, testFilesPath, 'DELETE');
    await test.step(`When DELETE participant is processed via storage`, async () => {
      await processFileViaStorage(parquetFile);
    });

    // Verify audit logs contain block flag update
    await test.step(`Then audit logs should contain block flag update`, async () => {
      await validateSqlDatabaseFromAPI(request, {
        apiEndpoint: 'BsSelectRequestAudit',
        nhsNumber: nhsNumbers[0],
        validations: {
          BlockedFlag: 1,
          ActionType: 'Delete',
          Status: 'Rejected',
          Details: 'Participant is marked as blocked'
        }
      });
    });

    // Double check participant management still shows blocked
    await test.step('Then participant should remain blocked', async () => {
      const response = await getRecordsFromParticipantManagementService(request);
      expect(response.data[0].BlockedFlag).toBe(1);
    });

    // Verify the deletion did not affect cohort distribution
    await test.step('Then participant should not be present in cohort distribution', async () => {
      const response = await getRecordsFromCohortDistributionService(request);
      expect(response.status).toBe(204); // No content, indicating no records found
    });
  });
});
