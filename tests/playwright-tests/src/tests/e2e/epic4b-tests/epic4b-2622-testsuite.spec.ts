import { test, expect } from '@playwright/test';
import { createParquetFromJson } from '../../../parquet/parquet-multiplier';
import {
  getApiTestData,
  processFileViaStorage,
  cleanupDatabaseFromAPI,
  validateSqlDatabaseFromAPI,
} from '../../steps/steps';
import { composeValidators, expectStatus } from '../../../api/responseValidators';
import {
  BlockParticipant,
  deleteParticipant,
} from '../../../api/distributionService/bsSelectService';

function asArray<T>(x: T | T[] | undefined): T[] {
  if (!x) return [];
  return Array.isArray(x) ? x : [x];
}

function buildBlockPayload(nhsNumbers: string[], input: any[]) {
  const r0 = input[0] ?? {};
  return {
    NhsNumber: nhsNumbers[0],
    FamilyName: r0.family_name,
    DateOfBirth: r0.date_of_birth,
  };
}
function buildDeletePayload(nhsNumbers: string[], input: any[]) {
  const r0 = input[0] ?? {};
  const dob = String(r0.date_of_birth);
  const isoDob = `${dob.slice(0, 4)}-${dob.slice(4, 6)}-${dob.slice(6, 8)}`;
  return {
    NhsNumber: nhsNumbers[0],
    FamilyName: r0.family_name,
    DateOfBirth: isoDob,
  };
}

test.describe.serial('@regression @e2e @epic4b-2622', () => {

  // 7610 — ADD: blocked participant not processed to cohort
  test('@DTOSS-7610-01 ADD: blocked not processed to cohort', async ({ request }, testInfo) => {
    const [validations, inputParticipantRecord, nhsNumbers, testFilesPath] =
      await getApiTestData(testInfo.title);
    const input = asArray(inputParticipantRecord);

    await cleanupDatabaseFromAPI(request, nhsNumbers);

    // Ingest ADD
    const parquet = await createParquetFromJson(nhsNumbers, input, testFilesPath);
    await processFileViaStorage(parquet);

    // Block participant
    await BlockParticipant(request, buildBlockPayload(nhsNumbers, input));

    // Validate with helper
    await validateSqlDatabaseFromAPI(request, validations);
  });

  // 7614 — AMENDED: blocked participant not processed to cohort (Exception expected)
  test('@DTOSS-7614-01 AMENDED: blocked not processed to cohort', async ({ request }, testInfo) => {
    const [validations, inputParticipantRecord, nhsNumbers, testFilesPath] =
      await getApiTestData(testInfo.title);
    const input = asArray(inputParticipantRecord);

    await cleanupDatabaseFromAPI(request, nhsNumbers);

    // Ingest AMENDED
    const parquet = await createParquetFromJson(nhsNumbers, input, testFilesPath);
    await processFileViaStorage(parquet);

    // Block via API
    await BlockParticipant(request, buildBlockPayload(nhsNumbers, input));

    await validateSqlDatabaseFromAPI(request, validations);
  });

  // 7615 — DELETE: blocked participant deletion not processed to cohort
  test('@DTOSS-7615-01 DELETE: blocked deletion not processed to cohort', async ({ request }, testInfo) => {
    const [validations, inputParticipantRecord, nhsNumbers, testFilesPath] =
      await getApiTestData(testInfo.title);
    const input = asArray(inputParticipantRecord);

    await cleanupDatabaseFromAPI(request, nhsNumbers);

    // Seed via ADD then block
    const addParquet = await createParquetFromJson(nhsNumbers, input, testFilesPath);
    await processFileViaStorage(addParquet);
    await BlockParticipant(request, buildBlockPayload(nhsNumbers, input));

    // Request DELETE
    const delResp = await deleteParticipant(request, buildDeletePayload(nhsNumbers, input));
    await composeValidators(expectStatus(200))(delResp);

    await validateSqlDatabaseFromAPI(request, validations);
  });

  // 7616 — ADD: no NBO exception for blocked participant
  test('@DTOSS-7616-01 ADD: no NBO exception for blocked participant', async ({ request }, testInfo) => {
    const [validations, inputParticipantRecord, nhsNumbers, testFilesPath] =
      await getApiTestData(testInfo.title);
    const input = asArray(inputParticipantRecord);

    await cleanupDatabaseFromAPI(request, nhsNumbers);

    const parquet = await createParquetFromJson(nhsNumbers, input, testFilesPath);
    await processFileViaStorage(parquet);

    await BlockParticipant(request, buildBlockPayload(nhsNumbers, input));

    await validateSqlDatabaseFromAPI(request, validations);
  });

  // 7660 — AMENDED: no NBO exception for blocked participant
  test('@DTOSS-7660-01 AMENDED: no NBO exception for blocked participant', async ({ request }, testInfo) => {
    const [validations, inputParticipantRecord, nhsNumbers, testFilesPath] =
      await getApiTestData(testInfo.title);
    const input = asArray(inputParticipantRecord);

    await cleanupDatabaseFromAPI(request, nhsNumbers);

    const parquet = await createParquetFromJson(nhsNumbers, input, testFilesPath);
    await processFileViaStorage(parquet);

    await BlockParticipant(request, buildBlockPayload(nhsNumbers, input));

    await validateSqlDatabaseFromAPI(request, validations);
  });

  // 7661 — DELETE: no NBO exception for blocked participant
  test('@DTOSS-7661-01 DELETE: no NBO exception for blocked participant', async ({ request }, testInfo) => {
    const [validations, inputParticipantRecord, nhsNumbers, testFilesPath] =
      await getApiTestData(testInfo.title);
    const input = asArray(inputParticipantRecord);

    await cleanupDatabaseFromAPI(request, nhsNumbers);

    const addParquet = await createParquetFromJson(nhsNumbers, input, testFilesPath);
    await processFileViaStorage(addParquet);
    await BlockParticipant(request, buildBlockPayload(nhsNumbers, input));

    const delResp = await deleteParticipant(request, buildDeletePayload(nhsNumbers, input));
    await composeValidators(expectStatus(200))(delResp);

    await validateSqlDatabaseFromAPI(request, validations);
  });

  // 7663 — ADD: blocked ineligible participant becomes eligible (no NBO)
  test('@DTOSS-7663-01 ADD: blocked ineligible becomes eligible (no NBO)', async ({ request }, testInfo) => {
    const [validations, inputParticipantRecord, nhsNumbers, testFilesPath] =
      await getApiTestData(testInfo.title);
    const input = asArray(inputParticipantRecord);

    await cleanupDatabaseFromAPI(request, nhsNumbers);

    // Seed as INELIGIBLE first (EligibilityFlag: "0"), then block via API
    const seed = [{ ...input[0], EligibilityFlag: '0' }];
    const seedParquet = await createParquetFromJson(nhsNumbers, seed, testFilesPath);
    await processFileViaStorage(seedParquet);

    await BlockParticipant(request, buildBlockPayload(nhsNumbers, seed));

    const runParquet = await createParquetFromJson(nhsNumbers, input, testFilesPath);
    await processFileViaStorage(runParquet);

    await validateSqlDatabaseFromAPI(request, validations);
  });

  // 7664 — ADD: audit logs updated for blocked participant
  test('@DTOSS-7664-01 ADD: audit logs updated for blocked participant', async ({ request }, testInfo) => {
    const [validations, inputParticipantRecord, nhsNumbers, testFilesPath] =
      await getApiTestData(testInfo.title);
    const input = asArray(inputParticipantRecord);

    await cleanupDatabaseFromAPI(request, nhsNumbers);

    const parquet = await createParquetFromJson(nhsNumbers, input, testFilesPath);
    await processFileViaStorage(parquet);

    await BlockParticipant(request, buildBlockPayload(nhsNumbers, input));

    await validateSqlDatabaseFromAPI(request, validations);
  });

  // 7665 — AMENDED: audit logs updated for blocked participant
  test('@DTOSS-7665-01 AMENDED: audit logs updated for blocked participant', async ({ request }, testInfo) => {
    const [validations, inputParticipantRecord, nhsNumbers, testFilesPath] =
      await getApiTestData(testInfo.title);
    const input = asArray(inputParticipantRecord);

    await cleanupDatabaseFromAPI(request, nhsNumbers);

    const parquet = await createParquetFromJson(nhsNumbers, input, testFilesPath);
    await processFileViaStorage(parquet);

    await BlockParticipant(request, buildBlockPayload(nhsNumbers, input));

    await validateSqlDatabaseFromAPI(request, validations);
  });

  // 7666 — DELETE: audit logs updated for blocked participant
  test('@DTOSS-7666-01 DELETE: audit logs updated for blocked participant', async ({ request }, testInfo) => {
    const [validations, inputParticipantRecord, nhsNumbers, testFilesPath] =
      await getApiTestData(testInfo.title);
    const input = asArray(inputParticipantRecord);

    await cleanupDatabaseFromAPI(request, nhsNumbers);

    const addParquet = await createParquetFromJson(nhsNumbers, input, testFilesPath);
    await processFileViaStorage(addParquet);
    await BlockParticipant(request, buildBlockPayload(nhsNumbers, input));

    const delResp = await deleteParticipant(request, buildDeletePayload(nhsNumbers, input));
    await composeValidators(expectStatus(200))(delResp);

    await validateSqlDatabaseFromAPI(request, validations);
  });
});
