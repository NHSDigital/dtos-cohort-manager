import { expect, APIRequestContext, TestInfo } from '@playwright/test';
import { test, testWithAmended } from '../../fixtures/test-fixtures';
import { createParquetFromJson } from '../../../parquet/parquet-multiplier';
import {
  getApiTestData,
  getTestData,
  processFileViaStorage,
  validateSqlDatabaseFromAPI,
  cleanupDatabaseFromAPI
} from '../../steps/steps';
import {
  deleteParticipant,
  getRecordsFromParticipantManagementService
} from '../../../api/distributionService/bsSelectService';
import { expectStatus } from '../../../api/responseValidators';
import { TestHooks } from '../../hooks/test-hooks';
import { getRecordsFromCohortDistributionService } from '../../../api/dataService/cohortDistributionService';

const wait = (ms: number) => new Promise(res => setTimeout(res, ms));
async function firstPmRowOrTimeout(request: APIRequestContext, attempts = 8, waitMs = 2000): Promise<any> {
  let lastStatus = 0;
  for (let i = 0; i < attempts; i++) {
    const resp = await getRecordsFromParticipantManagementService(request);
    lastStatus = resp.status;
    const rows = Array.isArray(resp?.data) ? resp.data : [];
    if (lastStatus === 200 && rows.length > 0) return rows[0];
    await wait(waitMs);
  }
  throw new Error(`Timed out waiting for ParticipantManagement data. Last status=${lastStatus}`);
}

test.describe('@regression @e2e @epic4b-block-tests Tests', async () => {
  TestHooks.setupAllTestHooks();

  test('@DTOSS-7610-01 AC01 Verify block a participant not processed to COHORT - ADD', async ({ request }, testInfo) => {
    const [validations, inputParticipantRecord, nhsNumbers, testFilesPath] = await getApiTestData(testInfo.title);

    await cleanupDatabaseFromAPI(request, nhsNumbers);

    // Seed as blocked
    const seedPayload = { ...inputParticipantRecord[0], BlockedFlag: 1 };
    await processFileViaStorage(await createParquetFromJson(nhsNumbers, [seedPayload], testFilesPath));

    // Process ADD
    const parquetFile = await createParquetFromJson(nhsNumbers, inputParticipantRecord, testFilesPath);
    await processFileViaStorage(parquetFile);

    // PM shows blocked
    const row = await firstPmRowOrTimeout(request);
    expect(row.BlockedFlag).toBe(1);

    // Not present in cohort
    const cd = await getRecordsFromCohortDistributionService(request);
    expect(cd.status).toBe(204);

    await validateSqlDatabaseFromAPI(request, validations);
  });

  testWithAmended('@DTOSS-7614-01 AC01 Verify block a participant not processed to COHORT - Amend',
    async ({ request, testData }: { request: APIRequestContext; testData: any }, _testInfo: TestInfo) => {

      await cleanupDatabaseFromAPI(request, testData.nhsNumbers);

      const seedPayload = { ...testData.inputParticipantRecord[0], BlockedFlag: 1 };
      await processFileViaStorage(await createParquetFromJson(testData.nhsNumbers, [seedPayload], testData.testFilesPath));

      await processFileViaStorage(testData.runTimeParquetFileAmend);

      // Repo validations (ExceptionManagement + no cohort etc.)
      await validateSqlDatabaseFromAPI(request, testData.checkInDatabaseAmend);

      const cd = await getRecordsFromCohortDistributionService(request);
      expect(cd.status).toBe(204);
  });

  test('@DTOSS-7615-01 - AC1 - Verify blocked participant deletion is not processed to cohort distribution',
    async ({ request }, testInfo) => {

      // Use e2e DELETE JSON
      const [validations, nhsNumbers, _parquet, inputParticipantRecord, testFilesPath] =
        await getTestData(testInfo.title, 'DELETE');

      await cleanupDatabaseFromAPI(request, nhsNumbers);

      // Seed participant as blocked
      const seedPayload = { ...inputParticipantRecord![0], BlockedFlag: 1 };
      await processFileViaStorage(await createParquetFromJson(nhsNumbers, [seedPayload], testFilesPath!));

      // Invoke Delete function
      const deletePayload = {
        NhsNumber: nhsNumbers[0],
        FamilyName: inputParticipantRecord![0].family_name,
        DateOfBirth: `${inputParticipantRecord![0].date_of_birth.slice(0, 4)}-${inputParticipantRecord![0].date_of_birth.slice(4, 6)}-${inputParticipantRecord![0].date_of_birth.slice(6, 8)}`
      };
      const resp = await deleteParticipant(request, deletePayload);
      await expectStatus(200)(resp);

      // Not in cohort
      const cd = await getRecordsFromCohortDistributionService(request);
      expect(cd.status).toBe(204);

      // Use repo-provided validations JSON
      await validateSqlDatabaseFromAPI(request, validations);
  });

  test('@DTOSS-7616-01 AC02 Verify no NBO exception raised for blocked participant - ADD', async ({ request }, testInfo) => {

    const [validations, nhsNumbers, _parquet, inputParticipantRecord, testFilesPath] =
      await getTestData(testInfo.title, 'ADD');

    await cleanupDatabaseFromAPI(request, nhsNumbers);

    // Seed as blocked
    const seedPayload = { ...inputParticipantRecord![0], BlockedFlag: 1 };
    await processFileViaStorage(await createParquetFromJson(nhsNumbers, [seedPayload], testFilesPath!));

    // Process ADD
    const parquetFile = await createParquetFromJson(nhsNumbers, inputParticipantRecord!, testFilesPath!);
    await processFileViaStorage(parquetFile);

    // Use repo-provided validations JSON (includes nboExceptionCount handling)
    await validateSqlDatabaseFromAPI(request, validations);

    // PM remains blocked
    const row = await firstPmRowOrTimeout(request);
    expect(row.BlockedFlag).toBe(1);
  });

  test('@DTOSS-7660-01 AC02 Verify no NBO exception raised for blocked participant - Amend',
    async ({ request, testData }: { request: APIRequestContext; testData: any }) => {

      await cleanupDatabaseFromAPI(request, testData.nhsNumbers);

      // Seed blocked
      const seedPayload = { ...testData.inputParticipantRecord[0], BlockedFlag: 1 };
      await processFileViaStorage(await createParquetFromJson(testData.nhsNumbers, [seedPayload], testData.testFilesPath));

      // Process AMEND
      await processFileViaStorage(testData.runTimeParquetFileAmend);

      // Validations from repo JSON
      await validateSqlDatabaseFromAPI(request, testData.checkInDatabaseAmend);

      const cd = await getRecordsFromCohortDistributionService(request);
      expect(cd.status).toBe(204);
  });

  test('@DTOSS-7661-01 AC02 Verify no NBO exception raised for blocked participant - Delete', async ({ request }, testInfo) => {

    const [validations, inputParticipantRecord, nhsNumbers, testFilesPath] = await getApiTestData(testInfo.title, 'DELETE');

    await cleanupDatabaseFromAPI(request, nhsNumbers);

    // Seed as blocked
    const seedPayload = { ...inputParticipantRecord[0], BlockedFlag: 1 };
    await processFileViaStorage(await createParquetFromJson(nhsNumbers, [seedPayload], testFilesPath));

    // Delete
    const deletePayload = {
      NhsNumber: nhsNumbers[0],
      FamilyName: inputParticipantRecord[0].family_name,
      DateOfBirth: `${inputParticipantRecord[0].date_of_birth.slice(0, 4)}-${inputParticipantRecord[0].date_of_birth.slice(4, 6)}-${inputParticipantRecord[0].date_of_birth.slice(6, 8)}`
    };
    const resp = await deleteParticipant(request, deletePayload);
    await expectStatus(200)(resp);

    // Validations
    await validateSqlDatabaseFromAPI(request, validations);

    // PM remains blocked
    const row = await firstPmRowOrTimeout(request);
    expect(row.BlockedFlag).toBe(1);
  });

  test('@DTOSS-7663-01 AC03 Verify no NBO exception when blocked ineligible participant becomes eligible - ADD',
    async ({ request }, testInfo) => {
      const [validations, nhsNumbers, _parquet, inputParticipantRecord, testFilesPath] =
        await getTestData(testInfo.title, 'ADD');

      await cleanupDatabaseFromAPI(request, nhsNumbers);

      // Seed blocked + ineligible
      const seedPayload = { ...inputParticipantRecord![0], BlockedFlag: 1, EligibilityFlag: "0" };
      await processFileViaStorage(await createParquetFromJson(nhsNumbers, [seedPayload], testFilesPath!));

      // Process ADD
      const parquetFile = await createParquetFromJson(nhsNumbers, inputParticipantRecord!, testFilesPath!);
      await processFileViaStorage(parquetFile);

      // Use repo validations (audit + exception + cohort)
      await validateSqlDatabaseFromAPI(request, validations);
  });

  test('@DTOSS-7664-01 AC04 Verify audit logs are updated for blocked participant - ADD', async ({ request }, testInfo) => {

    const [validations, nhsNumbers, _parquet, inputParticipantRecord, testFilesPath] =
      await getTestData(testInfo.title, 'ADD');

    await cleanupDatabaseFromAPI(request, nhsNumbers);

    // Seed as blocked
    const seedPayload = { ...inputParticipantRecord![0], BlockedFlag: 1 };
    await processFileViaStorage(await createParquetFromJson(nhsNumbers, [seedPayload], testFilesPath!));

    // Process ADD
    const parquetFile = await createParquetFromJson(nhsNumbers, inputParticipantRecord!, testFilesPath!);
    await processFileViaStorage(parquetFile);

    // Audit validations from repo JSON
    await validateSqlDatabaseFromAPI(request, validations);
  });

  test('@DTOSS-7665-01 AC04 Verify audit logs are updated for blocked participant - AMEND',
    async ({ request, testData }: { request: APIRequestContext; testData: any }) => {

      await cleanupDatabaseFromAPI(request, testData.nhsNumbers);

      // Seed blocked
      const seedPayload = { ...testData.inputParticipantRecord[0], BlockedFlag: 1 };
      await processFileViaStorage(await createParquetFromJson(testData.nhsNumbers, [seedPayload], testData.testFilesPath));

      // AMEND
      await processFileViaStorage(testData.runTimeParquetFileAmend);

      // Audit validations
      await validateSqlDatabaseFromAPI(request, testData.checkInDatabaseAmend);

      // Still blocked
      const row = await firstPmRowOrTimeout(request);
      expect(row.BlockedFlag).toBe(1);
  });

  test('@DTOSS-7666-01 AC04 Verify audit logs are updated for blocked participant - DELETE', async ({ request }, testInfo) => {
    const [validations, inputParticipantRecord, nhsNumbers, testFilesPath] = await getApiTestData(testInfo.title, 'DELETE');

    await cleanupDatabaseFromAPI(request, nhsNumbers);

    // Seed blocked
    const seedPayload = { ...inputParticipantRecord[0], BlockedFlag: 1 };
    await processFileViaStorage(await createParquetFromJson(nhsNumbers, [seedPayload], testFilesPath));

    // Delete
    const deletePayload = {
      NhsNumber: nhsNumbers[0],
      FamilyName: inputParticipantRecord[0].family_name,
      DateOfBirth: `${inputParticipantRecord[0].date_of_birth.slice(0, 4)}-${inputParticipantRecord[0].date_of_birth.slice(4, 6)}-${inputParticipantRecord[0].date_of_birth.slice(6, 8)}`
    };
    const resp = await deleteParticipant(request, deletePayload);
    await expectStatus(200)(resp);

    // Audit validations
    await validateSqlDatabaseFromAPI(request, validations);

    // Remains blocked
    const row = await firstPmRowOrTimeout(request);
    expect(row.BlockedFlag).toBe(1);
  });
});
