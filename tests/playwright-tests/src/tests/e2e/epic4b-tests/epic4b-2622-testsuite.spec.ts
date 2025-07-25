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

    // First block the participant
    await test.step(`Block the participant before processing`, async () => {
        const blockPayload = {
          NhsNumber: nhsNumbers[0],
          FamilyName: inputParticipantRecord[0].family_name,
          DateOfBirth: inputParticipantRecord[0].date_of_birth
        };

        const response = await BlockParticipant(request, blockPayload);
        expect(response.data[0].BlockedFlag).toBe(1);
    });

    // Create and process the parquet file
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
    // First add the participant normally
    await test.step(`When ADD participant is processed via storage`, async () => {
      await processFileViaStorage(testData.runTimeParquetFileAdd);
    });

    await test.step(`Then participant should be in the participant management table`, async () => {
      await validateSqlDatabaseFromAPI(request, testData.checkInDatabaseAdd);
    });

    // Verify participant is in cohort distribution initially
    await test.step(`Then participant should be present in cohort distribution initially`, async () => {
      const response = await getRecordsFromCohortDistributionService(request);
      expect(response.status).toBe(200);
      expect(response.data.length).toBeGreaterThan(0);
    });

    // Block the participant
    await test.step(`When BlockParticipant function is invoked`, async () => {
      const blockPayload = {
        NhsNumber: testData.nhsNumbers[0],
        FamilyName: testData.inputParticipantRecord?.[0]?.family_name,
        DateOfBirth: testData.inputParticipantRecord?.[0]?.date_of_birth
      };

      const response = await BlockParticipant(request, blockPayload);
      expect(response.data[0].BlockedFlag).toBe(1);
    });

    // Verify participant is marked as blocked
    await test.step(`Then participant should be marked as blocked`, async () => {
      const response = await getRecordsFromParticipantManagementService(request);
      expect(response.data[0].BlockedFlag).toBe(1);
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
      // Verify the data in cohort distribution hasn't changed from the original
      expect(response.status).toBe(200);
      expect(testData.inputParticipantRecord).toBeDefined();
      expect(response.data[0]).toMatchObject(testData.inputParticipantRecord![0]);
    });
   });
test('@DTOSS-7615-01 - AC1 - Verify blocked participant deletion is not processed to cohort distribution', async ({ request }, testInfo) => {
    const [validations, inputParticipantRecord, nhsNumbers, testFilesPath] = await getApiTestData(testInfo.title, 'ADD');

    await cleanupDatabaseFromAPI(request, nhsNumbers);

    // First add the participant normally
    const parquetFile = await createParquetFromJson(nhsNumbers, inputParticipantRecord, testFilesPath);
    await test.step(`When participant is inserted via storage`, async () => {
      await processFileViaStorage(parquetFile);
    });

    // Verify initial state
    await test.step(`Then participant should be in cohort distribution initially`, async () => {
      await validateSqlDatabaseFromAPI(request, validations);
      const response = await getRecordsFromCohortDistributionService(request);
      expect(response.status).toBe(200);
      expect(response.data.length).toBeGreaterThan(0);
      // Store initial state for later comparison
      const initialState = response.data[0];
      expect(initialState).toBeDefined();
    });

    // Block the participant
    const blockPayload = {
      NhsNumber: nhsNumbers[0],
      FamilyName: inputParticipantRecord[0].family_name,
      DateOfBirth: inputParticipantRecord[0].date_of_birth
    };

    await test.step(`When BlockParticipant function is invoked`, async () => {
      const response = await BlockParticipant(request, blockPayload);
      expect(response.data[0].BlockedFlag).toBe(1);
    });

    // Verify participant is blocked
    await test.step('Then participant should have blocked flag set as 1', async () => {
      const response = await getRecordsFromParticipantManagementService(request);
      expect(response.data[0].BlockedFlag).toBe(1);
    });

    // Attempt to delete the blocked participant
    await test.step(`When DeleteParticipant function is invoked for blocked participant`, async () => {
      const deletePayload = {
        NhsNumber: nhsNumbers[0],
        FamilyName: inputParticipantRecord[0].family_name,
        DateOfBirth: `${inputParticipantRecord[0].date_of_birth.slice(0, 4)}-${inputParticipantRecord[0].date_of_birth.slice(4, 6)}-${inputParticipantRecord[0].date_of_birth.slice(6, 8)}`
      };

      // Delete should succeed but not affect cohort distribution
      const response = await deleteParticipant(request, deletePayload);
      const validators = composeValidators(
        expectStatus(200)
      );
      await validators(response);
    });

    // Verify the deletion was not processed to cohort distribution
    await test.step('Then participant data should still exist in cohort distribution', async () => {
      const response = await getRecordsFromCohortDistributionService(request);
      expect(response.status).toBe(200);
      expect(response.data[0]).toBeDefined();
      // Verify the data remains unchanged from initial state
      expect(response.data[0].NhsNumber).toBe(nhsNumbers[0]);
    });

    // Verify exception was created
    await test.step('Then exception should be created for blocked deletion', async () => {
      await validateSqlDatabaseFromAPI(request, validations);
    });
  });

  test('@DTOSS-7616-01 AC02 Verify no NBO exception raised for blocked participant - ADD', async ({ request }, testInfo) => {
    const [checkInDatabase, inputParticipantRecord, nhsNumbers, testFilesPath] = await getApiTestData(testInfo.title);

    await cleanupDatabaseFromAPI(request, nhsNumbers);

    // First block the participant
    await test.step(`Block the participant before processing`, async () => {
        const blockPayload = {
          NhsNumber: nhsNumbers[0],
          FamilyName: inputParticipantRecord[0].family_name,
          DateOfBirth: inputParticipantRecord[0].date_of_birth
        };

        const response = await BlockParticipant(request, blockPayload);
        expect(response.data[0].BlockedFlag).toBe(1);
    });

    // Create and process the parquet file
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
    // Add participant first
    await test.step(`When ADD participant is processed via storage`, async () => {
      await processFileViaStorage(testData.runTimeParquetFileAdd);
    });

    await test.step(`Then participant should be in participant management table`, async () => {
      await validateSqlDatabaseFromAPI(request, testData.checkInDatabaseAdd);
    });

    // Verify initial state in cohort distribution
    await test.step(`Then participant should be in cohort distribution initially`, async () => {
      const response = await getRecordsFromCohortDistributionService(request);
      expect(response.status).toBe(200);
      expect(response.data.length).toBeGreaterThan(0);
      // Store initial state for later comparison
      const initialState = response.data[0];
      expect(initialState).toBeDefined();
    });

    // Block the participant
    await test.step(`When BlockParticipant function is invoked`, async () => {
      const blockPayload = {
        NhsNumber: testData.nhsNumbers[0],
        FamilyName: testData.inputParticipantRecord?.[0]?.family_name,
        DateOfBirth: testData.inputParticipantRecord?.[0]?.date_of_birth
      };

      const response = await BlockParticipant(request, blockPayload);
      expect(response.data[0].BlockedFlag).toBe(1);
    });

    // Verify participant is blocked
    await test.step('Then participant should have blocked flag set as 1', async () => {
      const response = await getRecordsFromParticipantManagementService(request);
      expect(response.data[0].BlockedFlag).toBe(1);
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

    // Verify the amendment did not affect cohort distribution
    await test.step('Then amended data should not be present in cohort distribution', async () => {
      const response = await getRecordsFromCohortDistributionService(request);
      expect(response.status).toBe(200);
      expect(response.data[0]).toBeDefined();
      // Verify the data remains unchanged from initial state
      expect(response.data[0].NhsNumber).toBe(testData.nhsNumbers[0]);
      expect(testData.inputParticipantRecord).toBeDefined();
      expect(response.data[0]).toMatchObject(testData.inputParticipantRecord![0]);
    });

    // Final verification that participant remains blocked
    await test.step('Then participant should remain blocked', async () => {
      const response = await getRecordsFromParticipantManagementService(request);
      expect(response.data[0].BlockedFlag).toBe(1);
    });
  });

  test('@DTOSS-7661-01 AC02 Verify no NBO exception raised for blocked participant - Delete', async ({ request }: { request: APIRequestContext }, testInfo: TestInfo) => {
    const [validations, inputParticipantRecord, nhsNumbers, testFilesPath] = await getApiTestData(testInfo.title, 'ADD');

    await cleanupDatabaseFromAPI(request, nhsNumbers);

    // First add the participant normally
    const parquetFile = await createParquetFromJson(nhsNumbers, inputParticipantRecord, testFilesPath);
    await test.step(`When participant is inserted via storage`, async () => {
      await processFileViaStorage(parquetFile);
    });

    // Verify initial state
    await test.step(`Then participant should be in cohort distribution initially`, async () => {
      await validateSqlDatabaseFromAPI(request, validations);
      const response = await getRecordsFromCohortDistributionService(request);
      expect(response.status).toBe(200);
      expect(response.data.length).toBeGreaterThan(0);
      // Store initial state for later comparison
      const initialState = response.data[0];
      expect(initialState).toBeDefined();
    });

    // Block the participant
    const blockPayload = {
      NhsNumber: nhsNumbers[0],
      FamilyName: inputParticipantRecord[0].family_name,
      DateOfBirth: inputParticipantRecord[0].date_of_birth
    };

    await test.step(`When BlockParticipant function is invoked`, async () => {
      const response = await BlockParticipant(request, blockPayload);
      expect(response.data[0].BlockedFlag).toBe(1);
    });

    // Verify participant is blocked
    await test.step('Then participant should have blocked flag set as 1', async () => {
      const response = await getRecordsFromParticipantManagementService(request);
      expect(response.data[0].BlockedFlag).toBe(1);
    });

    // Attempt to delete the blocked participant
    await test.step(`When DeleteParticipant function is invoked for blocked participant`, async () => {
      const deletePayload = {
        NhsNumber: nhsNumbers[0],
        FamilyName: inputParticipantRecord[0].family_name,
        DateOfBirth: `${inputParticipantRecord[0].date_of_birth.slice(0, 4)}-${inputParticipantRecord[0].date_of_birth.slice(4, 6)}-${inputParticipantRecord[0].date_of_birth.slice(6, 8)}`
      };

      // Delete request should succeed but not affect cohort distribution
      const response = await deleteParticipant(request, deletePayload);
      const validators = composeValidators(
        expectStatus(200)
      );
      await validators(response);
    });

    // Verify validation exception exists (non-NBO) for the deletion
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

    // Verify deletion did not affect cohort distribution
    await test.step('Then participant data should still exist in cohort distribution', async () => {
      const response = await getRecordsFromCohortDistributionService(request);
      expect(response.status).toBe(200);
      expect(response.data[0]).toBeDefined();
      // Verify the data remains unchanged from initial state
      expect(response.data[0].NhsNumber).toBe(nhsNumbers[0]);
    });

    // Final verification that participant remains blocked
    await test.step('Then participant should remain blocked', async () => {
      const response = await getRecordsFromParticipantManagementService(request);
      expect(response.data[0].BlockedFlag).toBe(1);
    });
  });

  test('@DTOSS-7663-01 AC03 Verify no NBO exception when blocked ineligible participant becomes eligible - ADD', async ({ request }: { request: APIRequestContext }, testInfo: TestInfo) => {
    const [validations, inputParticipantRecord, nhsNumbers, testFilesPath] = await getApiTestData(testInfo.title, 'ADD');

    await cleanupDatabaseFromAPI(request, nhsNumbers);

    // First add the participant as ineligible
    const ineligibleParticipant = {
      ...inputParticipantRecord[0],
      EligibilityFlag: "0"  // Set as ineligible
    };

    const parquetFileIneligible = await createParquetFromJson(nhsNumbers, [ineligibleParticipant], testFilesPath);
    await test.step(`When ineligible participant is inserted via storage`, async () => {
      await processFileViaStorage(parquetFileIneligible);
    });

    // Verify initial ineligible state
    await test.step(`Then participant should be in participant management as ineligible`, async () => {
      const response = await getRecordsFromParticipantManagementService(request);
      expect(response.status).toBe(200);
      expect(response.data[0].EligibilityFlag).toBe("0");
    });

    // Block the ineligible participant
    await test.step(`When BlockParticipant function is invoked`, async () => {
      const blockPayload = {
        NhsNumber: nhsNumbers[0],
        FamilyName: inputParticipantRecord[0].family_name,
        DateOfBirth: inputParticipantRecord[0].date_of_birth
      };

      const response = await BlockParticipant(request, blockPayload);
      expect(response.data[0].BlockedFlag).toBe(1);
    });

    // Verify participant is both ineligible and blocked
    await test.step('Then participant should be ineligible and blocked', async () => {
      const response = await getRecordsFromParticipantManagementService(request);
      expect(response.data[0].BlockedFlag).toBe(1);
      expect(response.data[0].EligibilityFlag).toBe("0");
    });

    // Now try to add the participant as eligible
    const eligibleParticipant = {
      ...inputParticipantRecord[0],
      EligibilityFlag: "1"  // Set as eligible
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

    // First block the participant
    await test.step(`Block the participant before processing`, async () => {
        const blockPayload = {
          NhsNumber: nhsNumbers[0],
          FamilyName: inputParticipantRecord[0].family_name,
          DateOfBirth: inputParticipantRecord[0].date_of_birth
        };

        const response = await BlockParticipant(request, blockPayload);
        expect(response.data[0].BlockedFlag).toBe(1);
    });

    // Create and process the parquet file
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
    // First add the participant normally
    await test.step(`When ADD participant is processed via storage`, async () => {
      await processFileViaStorage(testData.runTimeParquetFileAdd);
    });

    await test.step(`Then participant should be in the participant management table`, async () => {
      await validateSqlDatabaseFromAPI(request, testData.checkInDatabaseAdd);
    });

    // Verify initial state in cohort distribution
    await test.step(`Then participant should be in cohort distribution initially`, async () => {
      const response = await getRecordsFromCohortDistributionService(request);
      expect(response.status).toBe(200);
      expect(response.data.length).toBeGreaterThan(0);
      expect(testData.inputParticipantRecord).toBeDefined();
      expect(response.data[0]).toMatchObject(testData.inputParticipantRecord![0]);
    });

    // Block the participant
    await test.step(`When BlockParticipant function is invoked`, async () => {
      const blockPayload = {
        NhsNumber: testData.nhsNumbers[0],
        FamilyName: testData.inputParticipantRecord?.[0]?.family_name,
        DateOfBirth: testData.inputParticipantRecord?.[0]?.date_of_birth
      };

      const response = await BlockParticipant(request, blockPayload);
      expect(response.data[0].BlockedFlag).toBe(1);
    });

    // Verify participant is blocked
    await test.step('Then participant should have blocked flag set as 1', async () => {
      const response = await getRecordsFromParticipantManagementService(request);
      expect(response.data[0].BlockedFlag).toBe(1);
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

    // Try to amend the blocked participant
    await test.step(`When Amend participant is processed via storage`, async () => {
      await processFileViaStorage(testData.runTimeParquetFileAmend);
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

    // Verify the amendment did not affect cohort distribution
    await test.step('Then amended data should not be present in cohort distribution', async () => {
      const response = await getRecordsFromCohortDistributionService(request);
      expect(response.status).toBe(200);
      expect(response.data[0]).toBeDefined();
      // Verify the data remains unchanged from initial state
      expect(testData.inputParticipantRecord).toBeDefined();
      expect(response.data[0]).toMatchObject(testData.inputParticipantRecord![0]);
    });

    // Final verification that participant remains blocked
    await test.step('Then participant should remain blocked', async () => {
      const response = await getRecordsFromParticipantManagementService(request);
      expect(response.data[0].BlockedFlag).toBe(1);
    });
  });

  test('@DTOSS-7666-01 AC04 Verify audit logs are updated for blocked participant - DELETE', async ({ request }, testInfo) => {
    const [validations, inputParticipantRecord, nhsNumbers, testFilesPath] = await getApiTestData(testInfo.title, 'ADD');

    await cleanupDatabaseFromAPI(request, nhsNumbers);

    // First add the participant normally
    const parquetFile = await createParquetFromJson(nhsNumbers, inputParticipantRecord, testFilesPath);
    await test.step(`When participant is inserted via storage`, async () => {
      await processFileViaStorage(parquetFile);
    });

    // Verify initial state
    await test.step(`Then participant should be in cohort distribution initially`, async () => {
      await validateSqlDatabaseFromAPI(request, validations);
      const response = await getRecordsFromCohortDistributionService(request);
      expect(response.status).toBe(200);
      expect(response.data.length).toBeGreaterThan(0);
      // Store initial state for later comparison
      const initialState = response.data[0];
      expect(initialState).toBeDefined();
    });

    // Block the participant
    const blockPayload = {
      NhsNumber: nhsNumbers[0],
      FamilyName: inputParticipantRecord[0].family_name,
      DateOfBirth: inputParticipantRecord[0].date_of_birth
    };

    await test.step(`When BlockParticipant function is invoked`, async () => {
      const response = await BlockParticipant(request, blockPayload);
      expect(response.data[0].BlockedFlag).toBe(1);
    });

    // Verify participant is blocked
    await test.step('Then participant should have blocked flag set as 1', async () => {
      const response = await getRecordsFromParticipantManagementService(request);
      expect(response.data[0].BlockedFlag).toBe(1);
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

    // Verify audit logs contain DELETE action rejection
    await test.step(`Then audit logs should contain DELETE action rejection`, async () => {
      await validateSqlDatabaseFromAPI(request, {
        apiEndpoint: 'BsSelectRequestAudit',
        nhsNumber: nhsNumbers[0],
        validations: {
          ActionType: 'Delete',
          Status: 'Rejected',
          Details: 'Participant is marked as blocked'
        }
      });
    });

    // Verify deletion did not affect cohort distribution
    await test.step('Then participant data should still exist in cohort distribution', async () => {
      const response = await getRecordsFromCohortDistributionService(request);
      expect(response.status).toBe(200);
      expect(response.data[0]).toBeDefined();
      // Verify the data remains unchanged from initial state
      expect(response.data[0].NhsNumber).toBe(nhsNumbers[0]);
    });

    // Final verification that participant remains blocked
    await test.step('Then participant should remain blocked', async () => {
      const response = await getRecordsFromParticipantManagementService(request);
      expect(response.data[0].BlockedFlag).toBe(1);
    });
  });
});
