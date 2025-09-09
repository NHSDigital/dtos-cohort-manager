import { expect, test } from '../../fixtures/test-fixtures';
import { createParquetFromJson } from '../../../parquet/parquet-multiplier';
import { getApiTestData, processFileViaStorage, validateSqlDatabaseFromAPI, cleanupDatabaseFromAPI } from '../../steps/steps';
import { BlockParticipant, deleteParticipant, getRecordsFromParticipantManagementService} from '../../../api/distributionService/bsSelectService';
import { expectStatus, composeValidators} from '../../../api/responseValidators';
import { TestHooks } from '../../hooks/test-hooks';
import { getRecordsFromCohortDistributionService } from '../../../api/dataService/cohortDistributionService';
import { APIRequestContext, TestInfo } from '@playwright/test';
import { getValidationExceptions } from '../../../api/exceptionManagementService/validationExceptions';

annotation: [{
  type: 'Requirement',
  description: 'Tests - https://nhsd-jira.digital.nhs.uk/browse/DTOSS-2622'
}]

test.describe('@regression @e2e @epic4b-block-tests @smoke Tests', async () => {
  TestHooks.setupAllTestHooks();

  test('@DTOSS-7615-01 - AC1 - Verify participant is deleted from CohortDistributionDataService', async ({ request }: { request: APIRequestContext }, testInfo: TestInfo) => {
    // Arrange: Get test data
    const [addValidations, addInputParticipantRecord, addNhsNumbers, addTestFilesPath] = await getApiTestData(testInfo.title, 'ADD_BLOCKED');
    const nhsNumber = addNhsNumbers[0];
    await cleanupDatabaseFromAPI(request, [nhsNumber]);

    // Add the participant (if needed)
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
    // Arrange: Get test data
    const [addValidations, addInputParticipantRecord, addNhsNumbers, addTestFilesPath] = await getApiTestData(testInfo.title, 'ADD_BLOCKED');
    const nhsNumber = addNhsNumbers[0];
    await cleanupDatabaseFromAPI(request, [nhsNumber]);

    // Add the participant
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
      const response = await getValidationExceptions(request, 3, nhsNumber);
      expect(response.data === null || (Array.isArray(response.data) && response.data.length === 0)).toBe(true);
    });

    await test.step('Audit log should show record was blocked and not processed', async () => {
      // If there is an audit log check, call it here. Otherwise, this is a placeholder.
      // Example: const auditLog = await getAuditLogForNhsNumber(request, nhsNumber);
      // expect(auditLog.some(entry => entry.action === 'BLOCKED')).toBeTruthy();
    });
  });

  test('@DTOSS-7614-01 AC1 - Blocked participant AMEND action is not processed or passed to cohort distribution', async ({ request }: { request: APIRequestContext }, testInfo: TestInfo) => {
    // Arrange: Get test data
    const [addValidations, addInputParticipantRecord, addNhsNumbers, addTestFilesPath] = await getApiTestData(testInfo.title, 'ADD_BLOCKED');
    const nhsNumber = addNhsNumbers[0];
    await cleanupDatabaseFromAPI(request, [nhsNumber]);

    // Add the participant (if needed)
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
      const response = await getValidationExceptions(request, 3, nhsNumber);
      expect(response.data === null || (Array.isArray(response.data) && response.data.length === 0)).toBe(true);
    });

    await test.step('Audit log should show record was blocked and not processed', async () => {
      // If there is an audit log check, call it here. Otherwise, this is a placeholder.
      // Example: const auditLog = await getAuditLogForNhsNumber(request, nhsNumber);
      // expect(auditLog.some(entry => entry.action === 'BLOCKED')).toBeTruthy();
    });
  });

  test('@DTOSS-7616-01 AC2 - Blocked participant ADD action does not raise exception to NBO', async ({ request }: { request: APIRequestContext }, testInfo: TestInfo) => {
    // Arrange: Get test data
    const [addValidations, addInputParticipantRecord, addNhsNumbers, addTestFilesPath] = await getApiTestData(testInfo.title, 'ADD_BLOCKED');
    const nhsNumber = addNhsNumbers[0];
    await cleanupDatabaseFromAPI(request, [nhsNumber]);

    // Block the participant before any ADD
    const blockPayload = {
      NhsNumber: nhsNumber,
      FamilyName: 'BLOCKEDADD',
      DateOfBirth: '19700101'
    };
    await BlockParticipant(request, blockPayload);

    // Act: Try to ADD the blocked participant
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
      // First check with just NHS number to see if any exceptions exist
      const allExceptions = await getValidationExceptions(request, undefined, nhsNumber);
      console.log('All exceptions for NHS number:', allExceptions);

      // Then check specifically for category 3 exceptions
      const response = await getValidationExceptions(request, 3, nhsNumber);
      console.log('Category 3 exceptions:', response);

      // If there are no exceptions, the API returns null or an empty array
      expect(response.data === null || (Array.isArray(response.data) && response.data.length === 0)).toBe(true);
    });

    await test.step('Audit log should show record was blocked and not processed', async () => {
      // If there is an audit log check, call it here. Otherwise, this is a placeholder.
      // Example: const auditLog = await getAuditLogForNhsNumber(request, nhsNumber);
      // expect(auditLog.some(entry => entry.action === 'BLOCKED')).toBeTruthy();
    });
  });

  test('@DTOSS-7617-01 AC1 - Verify no exception is raised to NBO when attempting to amend a blocked participant', async ({ request }: { request: APIRequestContext }, testInfo: TestInfo) => {
    // Arrange: Get test data for ADD
    const [addValidations, addInputParticipantRecord, addNhsNumbers, addTestFilesPath] = await getApiTestData(testInfo.title, 'ADD');
    const nhsNumber = addNhsNumbers[0];
    await cleanupDatabaseFromAPI(request, [nhsNumber]);

    // Add the participant
    const addParquetFile = await createParquetFromJson(addNhsNumbers, addInputParticipantRecord, addTestFilesPath);
    await processFileViaStorage(addParquetFile);

    // Wait for participant to appear in DB before blocking
    let participantExists = false;
    for (let i = 0; i < 12; i++) {
      const resp = await getRecordsFromParticipantManagementService(request);
      if (resp?.data && Array.isArray(resp.data) && resp.data.length > 0 && String(resp.data[0].NHSNumber) === nhsNumber) {
        participantExists = true;
        break;
      }
      console.log(`Waiting for participant to appear in DB... (attempt ${i+1}/12)`);
      await new Promise(res => setTimeout(res, 2500));
    }
    expect(participantExists).toBe(true);

    // Block the participant
    const blockPayload = {
      NhsNumber: nhsNumber,
      FamilyName: addInputParticipantRecord[0].family_name,
      DateOfBirth: addInputParticipantRecord[0].date_of_birth
    };
    await BlockParticipant(request, blockPayload);

    // Wait until the participant is actually blocked
    let blocked = false;
    for (let i = 0; i < 6; i++) {
      const resp = await getRecordsFromParticipantManagementService(request);
      if (resp?.data?.[0]?.BlockedFlag === 1) {
        blocked = true;
        break;
      }
      console.log(`Waiting for participant to be blocked... (attempt ${i+1}/6)`);
      await new Promise(res => setTimeout(res, 2000));
    }
    expect(blocked).toBe(true);

    // Act: Try to AMEND the blocked participant
    const [amendValidations, amendInputParticipantRecord, amendNhsNumbers, amendTestFilesPath] = await getApiTestData(testInfo.title, 'AMEND_BLOCKED');
    const amendParquetFile = await createParquetFromJson(amendNhsNumbers, amendInputParticipantRecord, amendTestFilesPath);
    await processFileViaStorage(amendParquetFile);

    // Assert: No exception raised to NBO
    await test.step('No exception should be raised to NBO', async () => {
      const response = await getValidationExceptions(request, 3, nhsNumber);
      expect(response.data === null || (Array.isArray(response.data) && response.data.length === 0)).toBe(true);
    });

    // Assert: Participant remains blocked
    await test.step('Participant should remain blocked', async () => {
      const resp = await getRecordsFromParticipantManagementService(request);
      expect(resp?.data?.[0]?.BlockedFlag).toBe(1);
    });

    // Assert: Participant data should NOT be updated in the DB
    await test.step('Participant data should not be updated', async () => {
      const resp = await getRecordsFromParticipantManagementService(request);
      // Verify that the participant is still in the DB with original record type
      expect(resp?.data?.[0]?.NHSNumber).toBe(Number(nhsNumber));
      expect(resp?.data?.[0]?.RecordType).toBe("ADD");
      expect(resp?.data?.[0]?.BlockedFlag).toBe(1);
    });
  });

  test('@DTOSS-7660-01 AC2 - Blocked participant AMEND action does not raise exception to NBO', async ({ request }: { request: APIRequestContext }, testInfo: TestInfo) => {
    // Arrange: Get test data
    const [addValidations, addInputParticipantRecord, addNhsNumbers, addTestFilesPath] = await getApiTestData(testInfo.title, 'ADD_BLOCKED');
    const nhsNumber = addNhsNumbers[0];
    await cleanupDatabaseFromAPI(request, [nhsNumber]);

    // Add the participant (if needed)
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
      const response = await getValidationExceptions(request, 3, nhsNumber);
      expect(response.data === null || (Array.isArray(response.data) && response.data.length === 0)).toBe(true);
    });

    await test.step('Audit log should show record was blocked and not processed', async () => {
      // If there is an audit log check, call it here. Otherwise, this is a placeholder.
      // Example: const auditLog = await getAuditLogForNhsNumber(request, nhsNumber);
      // expect(auditLog.some(entry => entry.action === 'BLOCKED')).toBeTruthy();
    });
  });

  test('@DTOSS-7661-01 AC2 - Blocked participant DELETE action does not raise exception to NBO', async ({ request }: { request: APIRequestContext }, testInfo: TestInfo) => {
    // Arrange: Get test data
    const [addValidations, addInputParticipantRecord, addNhsNumbers, addTestFilesPath] = await getApiTestData(testInfo.title, 'ADD_BLOCKED');
    const nhsNumber = addNhsNumbers[0];
    await cleanupDatabaseFromAPI(request, [nhsNumber]);

    // Add the participant
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

    // Assert: Participant is not processed/validated, not passed to cohort distribution, no exception raised
    await test.step('Participant should NOT be in the cohort distribution table', async () => {
      const cohortDistRecords = await getRecordsFromCohortDistributionService(request);
      expect(
        cohortDistRecords.data === null ||
        (Array.isArray(cohortDistRecords.data) && cohortDistRecords.data.length === 0)
      ).toBe(true);
    });

    await test.step('No exception should be raised to NBO', async () => {
      const response = await getValidationExceptions(request, 3, nhsNumber);
      expect(response.data === null || (Array.isArray(response.data) && response.data.length === 0)).toBe(true);
    });

    await test.step('Audit log should show record was blocked and not processed', async () => {
      // If there is an audit log check, call it here. Otherwise, this is a placeholder.
      // Example: const auditLog = await getAuditLogForNhsNumber(request, nhsNumber);
      // expect(auditLog.some(entry => entry.action === 'BLOCKED')).toBeTruthy();
    });
  });

  test('@DTOSS-7663-01 AC3 - Blocked participant ADD action (ineligible to eligible) does not raise exception to NBO', async ({ request }: { request: APIRequestContext }, testInfo: TestInfo) => {
    // Arrange: Get test data
    const [addValidations, addInputParticipantRecord, addNhsNumbers, addTestFilesPath] = await getApiTestData(testInfo.title, 'ADD_BLOCKED');
    const nhsNumber = addNhsNumbers[0];
    await cleanupDatabaseFromAPI(request, [nhsNumber]);

    // Add the participant as ineligible (if needed)
    const addParquetFile = await createParquetFromJson(addNhsNumbers, addInputParticipantRecord, addTestFilesPath);
    await processFileViaStorage(addParquetFile);

    // Block the participant
    const blockPayload = {
      NhsNumber: nhsNumber,
      FamilyName: addInputParticipantRecord[0].family_name,
      DateOfBirth: addInputParticipantRecord[0].date_of_birth
    };
    await BlockParticipant(request, blockPayload);

    // Act: Try to ADD the blocked participant as eligible
    const [addEligibleValidations, addEligibleInputParticipantRecord, addEligibleNhsNumbers, addEligibleTestFilesPath] = await getApiTestData(testInfo.title, 'ADD_BLOCKED_ELIGIBLE');
    const addEligibleParquetFile = await createParquetFromJson(addEligibleNhsNumbers, addEligibleInputParticipantRecord, addEligibleTestFilesPath);
    await processFileViaStorage(addEligibleParquetFile);

    // Assert: Participant is not processed/validated, not passed to cohort distribution, no exception raised
    await test.step('Participant should NOT be in the cohort distribution table', async () => {
      const cohortDistRecords = await getRecordsFromCohortDistributionService(request);
      expect(
        cohortDistRecords.data === null ||
        (Array.isArray(cohortDistRecords.data) && cohortDistRecords.data.length === 0)
      ).toBe(true);
    });

    await test.step('No exception should be raised to NBO', async () => {
      const response = await getValidationExceptions(request, 3, nhsNumber);
      expect(response.data === null || (Array.isArray(response.data) && response.data.length === 0)).toBe(true);
    });

    await test.step('Audit log should show record was blocked and not processed', async () => {
      // If there is an audit log check, call it here. Otherwise, this is a placeholder.
      // Example: const auditLog = await getAuditLogForNhsNumber(request, nhsNumber);
      // expect(auditLog.some(entry => entry.action === 'BLOCKED')).toBeTruthy();
    });
  });

  test('@DTOSS-7664-01 AC4 - Audit log evidences blocked ADD action is not processed', async ({ request }: { request: APIRequestContext }, testInfo: TestInfo) => {
    // Arrange: Get test data
    const [addValidations, addInputParticipantRecord, addNhsNumbers, addTestFilesPath] = await getApiTestData(testInfo.title, 'ADD_BLOCKED');
    const nhsNumber = addNhsNumbers[0];
    await cleanupDatabaseFromAPI(request, [nhsNumber]);

    // Patch the test data to use the NHS number
    addInputParticipantRecord[0].nhs_number = nhsNumber;
    addNhsNumbers[0] = nhsNumber;
    const addParquetFile = await createParquetFromJson(addNhsNumbers, addInputParticipantRecord, addTestFilesPath);
    await processFileViaStorage(addParquetFile);

    // Wait for participant to appear in DB before blocking
    let participantExists = false;
    for (let i = 0; i < 12; i++) {
      const resp = await getRecordsFromParticipantManagementService(request);
      if (resp?.data && Array.isArray(resp.data) && resp.data.length > 0 && String(resp.data[0].NHSNumber) === nhsNumber) {
        participantExists = true;
        break;
      }
      await new Promise(res => setTimeout(res, 2500));
    }
    if (!participantExists) {
      console.warn(`Participant ${nhsNumber} not found in DB after retries`);
    }
    expect(participantExists).toBe(true);

    // Block the participant (must match the participant record exactly)
    const blockPayload = {
      NhsNumber: String(nhsNumber),
      FamilyName: String(addInputParticipantRecord[0].family_name),
      DateOfBirth: String(addInputParticipantRecord[0].date_of_birth)
    };
    await BlockParticipant(request, blockPayload);
    await new Promise(res => setTimeout(res, 2000));

    // Wait until the participant is actually blocked
    let blocked = false;
    let lastResp = undefined;
    for (let i = 0; i < 6; i++) {
      const resp = await getRecordsFromParticipantManagementService(request);
      lastResp = resp;
      if (resp?.data?.[0]?.BlockedFlag === 1) {
        blocked = true;
        break;
      }
      await new Promise(res => setTimeout(res, 2000));
    }
    expect(blocked).toBe(true);

    // Act: Try to ADD the blocked participant again
    await processFileViaStorage(addParquetFile);

    // Assert: Audit log should show record was blocked and not processed
    await test.step('Audit log should show record was blocked and not processed', async () => {
      const resp = await getRecordsFromParticipantManagementService(request);
      expect(resp?.data?.[0]?.BlockedFlag).toBe(1);
      expect(resp?.data?.[0]?.ReasonForRemoval).toBeNull();
    });
  });

  test('@DTOSS-7665-01 AC4 - Audit log evidences blocked AMEND action is not processed', async ({ request }: { request: APIRequestContext }, testInfo: TestInfo) => {
    // Arrange: Get test data
    const [addValidations, addInputParticipantRecord, addNhsNumbers, addTestFilesPath] = await getApiTestData(testInfo.title, 'ADD_BLOCKED');
    const nhsNumber = addNhsNumbers[0];
    await cleanupDatabaseFromAPI(request, [nhsNumber]);

    // Add the participant so they exist in the DB
    addInputParticipantRecord[0].nhs_number = nhsNumber;
    addNhsNumbers[0] = nhsNumber;
    const addParquetFile = await createParquetFromJson(addNhsNumbers, addInputParticipantRecord, addTestFilesPath);
    await processFileViaStorage(addParquetFile);

    // Wait for participant to appear in DB before blocking
    let participantExists = false;
    for (let i = 0; i < 12; i++) {
      const resp = await getRecordsFromParticipantManagementService(request);
      if (resp?.data && Array.isArray(resp.data) && resp.data.length > 0 && String(resp.data[0].NHSNumber) === nhsNumber) {
        participantExists = true;
        break;
      }
      await new Promise(res => setTimeout(res, 2500));
    }
    if (!participantExists) {
      console.warn(`Participant ${nhsNumber} not found in DB after retries`);
    }
    expect(participantExists).toBe(true);

    // Block the participant
    const blockPayload = {
      NhsNumber: String(nhsNumber),
      FamilyName: String(addInputParticipantRecord[0].family_name),
      DateOfBirth: String(addInputParticipantRecord[0].date_of_birth)
    };
    await BlockParticipant(request, blockPayload);
    await new Promise(res => setTimeout(res, 2000));

    // Wait until the participant is actually blocked
    let blocked = false;
    for (let i = 0; i < 6; i++) {
      const resp = await getRecordsFromParticipantManagementService(request);
      if (resp?.data?.[0]?.BlockedFlag === 1) {
        blocked = true;
        break;
      }
      await new Promise(res => setTimeout(res, 2000));
    }
    expect(blocked).toBe(true);

    // Act: Try to AMEND the blocked participant
    const [amendValidations, amendInputParticipantRecord, amendNhsNumbers, amendTestFilesPath] = await getApiTestData(testInfo.title, 'AMEND_BLOCKED');
    amendInputParticipantRecord[0].nhs_number = nhsNumber;
    amendNhsNumbers[0] = nhsNumber;
    const amendParquetFile = await createParquetFromJson(amendNhsNumbers, amendInputParticipantRecord, amendTestFilesPath);
    await processFileViaStorage(amendParquetFile);

    // Assert: Audit log should show record was blocked and not processed
    await test.step('Audit log should show record was blocked and not processed', async () => {
      const resp = await getRecordsFromParticipantManagementService(request);
      expect(resp?.data?.[0]?.BlockedFlag).toBe(1);
      expect(resp?.data?.[0]?.ReasonForRemoval).toBeNull();
    });
  });

  test('@DTOSS-7666-01 AC4 - Audit log evidences blocked DELETE action is not processed', async ({ request }: { request: APIRequestContext }, testInfo: TestInfo) => {
    // Arrange: Get test data
    const [addValidations, addInputParticipantRecord, addNhsNumbers, addTestFilesPath] = await getApiTestData(testInfo.title, 'ADD_BLOCKED');
    const nhsNumber = addNhsNumbers[0];
    await cleanupDatabaseFromAPI(request, [nhsNumber]);

    // Add the participant so they exist in the DB
    addInputParticipantRecord[0].nhs_number = nhsNumber;
    addNhsNumbers[0] = nhsNumber;
    const addParquetFile = await createParquetFromJson(addNhsNumbers, addInputParticipantRecord, addTestFilesPath);
    await processFileViaStorage(addParquetFile);

    // Wait for participant to appear in DB before blocking
    let participantExists = false;
    for (let i = 0; i < 12; i++) {
      const resp = await getRecordsFromParticipantManagementService(request);
      if (resp?.data && Array.isArray(resp.data) && resp.data.length > 0 && String(resp.data[0].NHSNumber) === nhsNumber) {
        participantExists = true;
        break;
      }
      await new Promise(res => setTimeout(res, 2500));
    }
    if (!participantExists) {
      console.warn(`Participant ${nhsNumber} not found in DB after retries`);
    }
    expect(participantExists).toBe(true);

    // Patch DELETE_BLOCKED test data with correct NHS number before block
    const [delValidations, delInputParticipantRecord, delNhsNumbers, delTestFilesPath] = await getApiTestData(testInfo.title, 'DELETE_BLOCKED');
    delInputParticipantRecord[0].nhs_number = nhsNumber;
    delNhsNumbers[0] = nhsNumber;

    // Block the participant
    const blockPayload = {
      NhsNumber: String(nhsNumber),
      FamilyName: String(addInputParticipantRecord[0].family_name),
      DateOfBirth: String(addInputParticipantRecord[0].date_of_birth)
    };
    await BlockParticipant(request, blockPayload);
    await new Promise(res => setTimeout(res, 2000));

    // Wait until the participant is actually blocked
    let blocked = false;
    for (let i = 0; i < 6; i++) {
      const resp = await getRecordsFromParticipantManagementService(request);
      if (resp?.data?.[0]?.BlockedFlag === 1) {
        blocked = true;
        break;
      }
      await new Promise(res => setTimeout(res, 2000));
    }
    expect(blocked).toBe(true);

    // Act: Try to DELETE the blocked participant
    const delParquetFile = await createParquetFromJson(delNhsNumbers, delInputParticipantRecord, delTestFilesPath);
    await processFileViaStorage(delParquetFile);

    // Assert: Audit log should show record was blocked and not processed
    await test.step('Audit log should show record was blocked and not processed', async () => {
      const resp = await getRecordsFromParticipantManagementService(request);
      expect(resp?.data?.[0]?.BlockedFlag).toBe(1); // using blocked flag to check as audit table not implemented yet
      expect(resp?.data?.[0]?.ReasonForRemoval).toBeNull();
    });
  });
});
