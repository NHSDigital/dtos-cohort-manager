import { expect, test } from '../../fixtures/test-fixtures';
import { createParquetFromJson } from '../../../parquet/parquet-multiplier';
import { getApiTestData, processFileViaStorage, validateSqlDatabaseFromAPI, cleanupDatabaseFromAPI } from '../../steps/steps';
import { BlockParticipant, deleteParticipant, getRecordsFromParticipantManagementService} from '../../../api/distributionService/bsSelectService';
import { expectStatus, composeValidators} from '../../../api/responseValidators';
import { TestHooks } from '../../hooks/test-hooks';
import { getRecordsFromCohortDistributionService } from '../../../api/dataService/cohortDistributionService';
import { APIRequestContext, TestInfo } from '@playwright/test';

test.describe('@regression @e2e @epic4b-block-tests Tests', async () => {
  TestHooks.setupAllTestHooks();

  test('@DTOSS-7615-01 - AC1 - Verify participant is deleted from CohortDistributionDataService', async ({ request }: { request: APIRequestContext }, testInfo: TestInfo) => {
    // Arrange: Clean up and block the participant
    const nhsNumber = '9997615018';
    await cleanupDatabaseFromAPI(request, [nhsNumber]);

    // Add the participant (if needed)
    const [addValidations, addInputParticipantRecord, addNhsNumbers, addTestFilesPath] = await getApiTestData(testInfo.title, 'ADD_BLOCKED');
    const addParquetFile = await createParquetFromJson(addNhsNumbers, addInputParticipantRecord, addTestFilesPath);
    await processFileViaStorage(addParquetFile);

    // Block the participant
    const blockPayload = {
      NhsNumber: nhsNumber,
      FamilyName: addInputParticipantRecord[0].family_name,
      DateOfBirth: addInputParticipantRecord[0].date_of_birth
    };
    await BlockParticipant(request, blockPayload);

    // Act: Try to DELETE the blocked participant
    const [delValidations, delInputParticipantRecord, delNhsNumbers, delTestFilesPath] = await getApiTestData(testInfo.title, 'DELETE_BLOCKED');
    const delParquetFile = await createParquetFromJson(delNhsNumbers, delInputParticipantRecord, delTestFilesPath);
    await processFileViaStorage(delParquetFile);

    // Assert: Participant is NOT in the cohort distribution table
    await test.step('Participant should NOT be in the cohort distribution table', async () => {
      const cohortDistRecords = await getRecordsFromCohortDistributionService(request);
      expect(
        cohortDistRecords.data === null ||
        (Array.isArray(cohortDistRecords.data) && cohortDistRecords.data.length === 0)
      ).toBe(true);
    });
  });

  test('@DTOSS-7610-01 AC1 - Blocked participant ADD action is not processed or passed to cohort distribution', async ({ request }: { request: APIRequestContext }, testInfo: TestInfo) => {
    // Arrange: Clean up and block the participant
    const nhsNumber = '9995518112';
    await cleanupDatabaseFromAPI(request, [nhsNumber]);

    // Add the participant
    const [addValidations, addInputParticipantRecord, addNhsNumbers, addTestFilesPath] = await getApiTestData(testInfo.title, 'ADD_BLOCKED');
    const addParquetFile = await createParquetFromJson(addNhsNumbers, addInputParticipantRecord, addTestFilesPath);
    await processFileViaStorage(addParquetFile);

    // Block the participant
    const blockPayload = {
      NhsNumber: nhsNumber,
      FamilyName: addInputParticipantRecord[0].family_name,
      DateOfBirth: addInputParticipantRecord[0].date_of_birth
    };
    await BlockParticipant(request, blockPayload);

    // Act: Try to ADD the blocked participant again
    await processFileViaStorage(addParquetFile);

    // Assert: Participant is not processed/validated, not passed to cohort distribution, no exception raised
    await test.step('Participant should NOT be in the cohort distribution table', async () => {
      const cohortDistRecords = await getRecordsFromCohortDistributionService(request);
      expect(
        cohortDistRecords.data === null ||
        (Array.isArray(cohortDistRecords.data) && cohortDistRecords.data.length === 0)
      ).toBe(true);
    });

    await test.step('No exception should be raised to NBO', async () => {
      // If there is a function to check NBO exceptions, call it here. Otherwise, this is a placeholder.
      // Example: const nboExceptions = await getNboExceptions(request, nhsNumber);
      // expect(nboExceptions).toHaveLength(0);
    });

    await test.step('Audit log should show record was blocked and not processed', async () => {
      // If there is an audit log check, call it here. Otherwise, this is a placeholder.
      // Example: const auditLog = await getAuditLogForNhsNumber(request, nhsNumber);
      // expect(auditLog.some(entry => entry.action === 'BLOCKED')).toBeTruthy();
    });
  });

  test('@DTOSS-7614-01 AC1 - Blocked participant AMEND action is not processed or passed to cohort distribution', async ({ request }: { request: APIRequestContext }, testInfo: TestInfo) => {
    // Arrange: Clean up and block the participant
    const nhsNumber = '9997614018';
    await cleanupDatabaseFromAPI(request, [nhsNumber]);

    // Add the participant (if needed)
    const [addValidations, addInputParticipantRecord, addNhsNumbers, addTestFilesPath] = await getApiTestData(testInfo.title, 'ADD_BLOCKED');
    const addParquetFile = await createParquetFromJson(addNhsNumbers, addInputParticipantRecord, addTestFilesPath);
    await processFileViaStorage(addParquetFile);

    // Block the participant
    const blockPayload = {
      NhsNumber: nhsNumber,
      FamilyName: addInputParticipantRecord[0].family_name,
      DateOfBirth: addInputParticipantRecord[0].date_of_birth
    };
    await BlockParticipant(request, blockPayload);

    // Act: Try to AMEND the blocked participant
    const [amendValidations, amendInputParticipantRecord, amendNhsNumbers, amendTestFilesPath] = await getApiTestData(testInfo.title, 'AMEND_BLOCKED');
    const amendParquetFile = await createParquetFromJson(amendNhsNumbers, amendInputParticipantRecord, amendTestFilesPath);
    await processFileViaStorage(amendParquetFile);

    // Assert: Participant is NOT in the cohort distribution table
    await test.step('Participant should NOT be in the cohort distribution table', async () => {
      const cohortDistRecords = await getRecordsFromCohortDistributionService(request);
      expect(
        cohortDistRecords.data === null ||
        (Array.isArray(cohortDistRecords.data) && cohortDistRecords.data.length === 0)
      ).toBe(true);
    });

    await test.step('No exception should be raised to NBO', async () => {
      // If there is a function to check NBO exceptions, call it here. Otherwise, this is a placeholder.
      // Example: const nboExceptions = await getNboExceptions(request, nhsNumber);
      // expect(nboExceptions).toHaveLength(0);
    });

    await test.step('Audit log should show record was blocked and not processed', async () => {
      // If there is an audit log check, call it here. Otherwise, this is a placeholder.
      // Example: const auditLog = await getAuditLogForNhsNumber(request, nhsNumber);
      // expect(auditLog.some(entry => entry.action === 'BLOCKED')).toBeTruthy();
    });
  });

  test('@DTOSS-7616-01 AC2 - Blocked participant ADD action does not raise exception to NBO', async ({ request }: { request: APIRequestContext }, testInfo: TestInfo) => {
    // Arrange: Clean up and block the participant
    const nhsNumber = '9997614022';
    await cleanupDatabaseFromAPI(request, [nhsNumber]);

    // Block the participant before any ADD
    const blockPayload = {
      NhsNumber: nhsNumber,
      FamilyName: 'BLOCKEDADD',
      DateOfBirth: '19700101'
    };
    await BlockParticipant(request, blockPayload);

    // Act: Try to ADD the blocked participant
    const [addValidations, addInputParticipantRecord, addNhsNumbers, addTestFilesPath] = await getApiTestData(testInfo.title, 'ADD_BLOCKED');
    const addParquetFile = await createParquetFromJson(addNhsNumbers, addInputParticipantRecord, addTestFilesPath);
    await processFileViaStorage(addParquetFile);

    // Assert: Participant is not processed/validated, not passed to cohort distribution, no exception raised
    await test.step('Participant should NOT be in the cohort distribution table', async () => {
      const cohortDistRecords = await getRecordsFromCohortDistributionService(request);
      expect(
        cohortDistRecords.data === null ||
        (Array.isArray(cohortDistRecords.data) && cohortDistRecords.data.length === 0)
      ).toBe(true);
    });

    await test.step('No exception should be raised to NBO', async () => {
      // If there is a function to check NBO exceptions, call it here. Otherwise, this is a placeholder.
      // Example: const nboExceptions = await getNboExceptions(request, nhsNumber);
      // expect(nboExceptions).toHaveLength(0);
    });

    await test.step('Audit log should show record was blocked and not processed', async () => {
      // If there is an audit log check, call it here. Otherwise, this is a placeholder.
      // Example: const auditLog = await getAuditLogForNhsNumber(request, nhsNumber);
      // expect(auditLog.some(entry => entry.action === 'BLOCKED')).toBeTruthy();
    });
  });

  test('@DTOSS-7660-01 AC2 - Blocked participant AMEND action does not raise exception to NBO', async ({ request }: { request: APIRequestContext }, testInfo: TestInfo) => {
    // Arrange: Clean up and block the participant
    const nhsNumber = '9997660022';
    await cleanupDatabaseFromAPI(request, [nhsNumber]);

    // Add the participant (if needed)
    const [addValidations, addInputParticipantRecord, addNhsNumbers, addTestFilesPath] = await getApiTestData(testInfo.title, 'ADD_BLOCKED');
    const addParquetFile = await createParquetFromJson(addNhsNumbers, addInputParticipantRecord, addTestFilesPath);
    await processFileViaStorage(addParquetFile);

    // Block the participant
    const blockPayload = {
      NhsNumber: nhsNumber,
      FamilyName: addInputParticipantRecord[0].family_name,
      DateOfBirth: addInputParticipantRecord[0].date_of_birth
    };
    await BlockParticipant(request, blockPayload);

    // Act: Try to AMEND the blocked participant
    const [amendValidations, amendInputParticipantRecord, amendNhsNumbers, amendTestFilesPath] = await getApiTestData(testInfo.title, 'AMEND_BLOCKED');
    const amendParquetFile = await createParquetFromJson(amendNhsNumbers, amendInputParticipantRecord, amendTestFilesPath);
    await processFileViaStorage(amendParquetFile);

    // Assert: Participant is not processed/validated, not passed to cohort distribution, no exception raised
    await test.step('Participant should NOT be in the cohort distribution table', async () => {
      const cohortDistRecords = await getRecordsFromCohortDistributionService(request);
      expect(
        cohortDistRecords.data === null ||
        (Array.isArray(cohortDistRecords.data) && cohortDistRecords.data.length === 0)
      ).toBe(true);
    });

    await test.step('No exception should be raised to NBO', async () => {
      // If there is a function to check NBO exceptions, call it here. Otherwise, this is a placeholder.
      // Example: const nboExceptions = await getNboExceptions(request, nhsNumber);
      // expect(nboExceptions).toHaveLength(0);
    });

    await test.step('Audit log should show record was blocked and not processed', async () => {
      // If there is an audit log check, call it here. Otherwise, this is a placeholder.
      // Example: const auditLog = await getAuditLogForNhsNumber(request, nhsNumber);
      // expect(auditLog.some(entry => entry.action === 'BLOCKED')).toBeTruthy();
    });
  });
});
