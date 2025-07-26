import { test, expect, APIRequestContext } from '@playwright/test';
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
import { fetchApiResponse } from '../../../api/apiHelper';

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

/** Wait for row to appear for test NHS no after parquet upload */
async function waitForPmPresence(
  request: APIRequestContext,
  nhs: string,
  attempts = 20,
  waitMs = 5000
) {
  for (let i = 0; i < attempts; i++) {
    const resp = await fetchApiResponse('api/ParticipantManagementDataService', request);
    if (resp.status() === 200) {
      const body = await resp.json();
      if (Array.isArray(body) && body.some((r: any) => String(r.NHSNumber ?? r.NhsNumber) === String(nhs))) return;
    }
    await new Promise(r => setTimeout(r, waitMs));
  }
  throw new Error(`PM row not visible for NHS ${nhs} after ${attempts} attempts`);
}

/** After BlockParticipant, wait for PM row to show BlockedFlag:1 */
async function waitForPmBlockedFlag(
  request: APIRequestContext,
  nhs: string,
  attempts = 20,
  waitMs = 5000
) {
  for (let i = 0; i < attempts; i++) {
    const resp = await fetchApiResponse('api/ParticipantManagementDataService', request);
    if (resp.status() === 200) {
      const body = await resp.json();
      if (Array.isArray(body)) {
        const m = body.find((r: any) => String(r.NHSNumber ?? r.NhsNumber) === String(nhs));
        if (m && Number(m.BlockedFlag) === 1) return;
      }
    }
    await new Promise(r => setTimeout(r, waitMs));
  }
  throw new Error(`PM did not reflect BlockedFlag:1 for NHS ${nhs} after ${attempts} attempts`);
}

test.describe.serial('@regression @e2e @epic4b-2622', () => {
  test('@DTOSS-7610-01 ADD: blocked not processed to cohort', async ({ request }, testInfo) => {
    const [validations, inputParticipantRecord, nhsNumbers, testFilesPath] = await getApiTestData(testInfo.title);
    const input = asArray(inputParticipantRecord);

    await cleanupDatabaseFromAPI(request, nhsNumbers);

    const parquet = await createParquetFromJson(nhsNumbers, input, testFilesPath);
    await processFileViaStorage(parquet);

    await waitForPmPresence(request, nhsNumbers[0]);

    const blockResp = await BlockParticipant(request, buildBlockPayload(nhsNumbers, input));
    await composeValidators(expectStatus(200))(blockResp);
    await waitForPmBlockedFlag(request, nhsNumbers[0]);

    await validateSqlDatabaseFromAPI(request, validations);
  });

  test('@DTOSS-7614-01 AMENDED: blocked not processed to cohort', async ({ request }, testInfo) => {
    const [validations, inputParticipantRecord, nhsNumbers, testFilesPath] = await getApiTestData(testInfo.title);
    const input = asArray(inputParticipantRecord);

    await cleanupDatabaseFromAPI(request, nhsNumbers);

    const parquet = await createParquetFromJson(nhsNumbers, input, testFilesPath);
    await processFileViaStorage(parquet);

    await waitForPmPresence(request, nhsNumbers[0]);

    const blockResp = await BlockParticipant(request, buildBlockPayload(nhsNumbers, input));
    await composeValidators(expectStatus(200))(blockResp);
    await waitForPmBlockedFlag(request, nhsNumbers[0]);

    await validateSqlDatabaseFromAPI(request, validations);
  });

  test('@DTOSS-7615-01 DELETE: blocked deletion not processed to cohort', async ({ request }, testInfo) => {
    const [validations, inputParticipantRecord, nhsNumbers, testFilesPath] = await getApiTestData(testInfo.title);
    const input = asArray(inputParticipantRecord);

    await cleanupDatabaseFromAPI(request, nhsNumbers);

    const addParquet = await createParquetFromJson(nhsNumbers, input, testFilesPath);
    await processFileViaStorage(addParquet);

    await waitForPmPresence(request, nhsNumbers[0]);

    const blockResp = await BlockParticipant(request, buildBlockPayload(nhsNumbers, input));
    await composeValidators(expectStatus(200))(blockResp);
    await waitForPmBlockedFlag(request, nhsNumbers[0]);

    const delResp = await deleteParticipant(request, buildDeletePayload(nhsNumbers, input));
    await composeValidators(expectStatus(200))(delResp);

    await validateSqlDatabaseFromAPI(request, validations);
  });

  test('@DTOSS-7616-01 ADD: no NBO exception for blocked participant', async ({ request }, testInfo) => {
    const [validations, inputParticipantRecord, nhsNumbers, testFilesPath] = await getApiTestData(testInfo.title);
    const input = asArray(inputParticipantRecord);

    await cleanupDatabaseFromAPI(request, nhsNumbers);

    const parquet = await createParquetFromJson(nhsNumbers, input, testFilesPath);
    await processFileViaStorage(parquet);

    await waitForPmPresence(request, nhsNumbers[0]);

    const blockResp = await BlockParticipant(request, buildBlockPayload(nhsNumbers, input));
    await composeValidators(expectStatus(200))(blockResp);
    await waitForPmBlockedFlag(request, nhsNumbers[0]);

    await validateSqlDatabaseFromAPI(request, validations);
  });

  test('@DTOSS-7660-01 AMENDED: no NBO exception for blocked participant', async ({ request }, testInfo) => {
    const [validations, inputParticipantRecord, nhsNumbers, testFilesPath] = await getApiTestData(testInfo.title);
    const input = asArray(inputParticipantRecord);

    await cleanupDatabaseFromAPI(request, nhsNumbers);

    const parquet = await createParquetFromJson(nhsNumbers, input, testFilesPath);
    await processFileViaStorage(parquet);

    await waitForPmPresence(request, nhsNumbers[0]);

    const blockResp = await BlockParticipant(request, buildBlockPayload(nhsNumbers, input));
    await composeValidators(expectStatus(200))(blockResp);
    await waitForPmBlockedFlag(request, nhsNumbers[0]);

    await validateSqlDatabaseFromAPI(request, validations);
  });

  test('@DTOSS-7661-01 DELETE: no NBO exception for blocked participant', async ({ request }, testInfo) => {
    const [validations, inputParticipantRecord, nhsNumbers, testFilesPath] = await getApiTestData(testInfo.title);
    const input = asArray(inputParticipantRecord);

    await cleanupDatabaseFromAPI(request, nhsNumbers);

    const addParquet = await createParquetFromJson(nhsNumbers, input, testFilesPath);
    await processFileViaStorage(addParquet);

    await waitForPmPresence(request, nhsNumbers[0]);

    const blockResp = await BlockParticipant(request, buildBlockPayload(nhsNumbers, input));
    await composeValidators(expectStatus(200))(blockResp);
    await waitForPmBlockedFlag(request, nhsNumbers[0]);

    const delResp = await deleteParticipant(request, buildDeletePayload(nhsNumbers, input));
    await composeValidators(expectStatus(200))(delResp);

    await validateSqlDatabaseFromAPI(request, validations);
  });

  test('@DTOSS-7663-01 ADD: blocked ineligible becomes eligible (no NBO)', async ({ request }, testInfo) => {
    const [validations, inputParticipantRecord, nhsNumbers, testFilesPath] = await getApiTestData(testInfo.title);
    const input = asArray(inputParticipantRecord);

    await cleanupDatabaseFromAPI(request, nhsNumbers);

    // Seed ineligible first
    const seed = [{ ...input[0], EligibilityFlag: '0' }];
    const seedParquet = await createParquetFromJson(nhsNumbers, seed, testFilesPath);
    await processFileViaStorage(seedParquet);

    await waitForPmPresence(request, nhsNumbers[0]);

    const blockResp = await BlockParticipant(request, buildBlockPayload(nhsNumbers, seed));
    await composeValidators(expectStatus(200))(blockResp);
    await waitForPmBlockedFlag(request, nhsNumbers[0]);

    // Process original input
    const runParquet = await createParquetFromJson(nhsNumbers, input, testFilesPath);
    await processFileViaStorage(runParquet);

    await validateSqlDatabaseFromAPI(request, validations);
  });

  test('@DTOSS-7664-01 ADD: audit logs updated for blocked participant', async ({ request }, testInfo) => {
    const [validations, inputParticipantRecord, nhsNumbers, testFilesPath] = await getApiTestData(testInfo.title);
    const input = asArray(inputParticipantRecord);

    await cleanupDatabaseFromAPI(request, nhsNumbers);

    const parquet = await createParquetFromJson(nhsNumbers, input, testFilesPath);
    await processFileViaStorage(parquet);

    await waitForPmPresence(request, nhsNumbers[0]);

    const blockResp = await BlockParticipant(request, buildBlockPayload(nhsNumbers, input));
    await composeValidators(expectStatus(200))(blockResp);
    await waitForPmBlockedFlag(request, nhsNumbers[0]);

    await validateSqlDatabaseFromAPI(request, validations);
  });

  test('@DTOSS-7665-01 AMENDED: audit logs updated for blocked participant', async ({ request }, testInfo) => {
    const [validations, inputParticipantRecord, nhsNumbers, testFilesPath] = await getApiTestData(testInfo.title);
    const input = asArray(inputParticipantRecord);

    await cleanupDatabaseFromAPI(request, nhsNumbers);

    const parquet = await createParquetFromJson(nhsNumbers, input, testFilesPath);
    await processFileViaStorage(parquet);

    await waitForPmPresence(request, nhsNumbers[0]);

    const blockResp = await BlockParticipant(request, buildBlockPayload(nhsNumbers, input));
    await composeValidators(expectStatus(200))(blockResp);
    await waitForPmBlockedFlag(request, nhsNumbers[0]);

    await validateSqlDatabaseFromAPI(request, validations);
  });

  test('@DTOSS-7666-01 DELETE: audit logs updated for blocked participant', async ({ request }, testInfo) => {
    const [validations, inputParticipantRecord, nhsNumbers, testFilesPath] = await getApiTestData(testInfo.title);
    const input = asArray(inputParticipantRecord);

    await cleanupDatabaseFromAPI(request, nhsNumbers);

    const addParquet = await createParquetFromJson(nhsNumbers, input, testFilesPath);
    await processFileViaStorage(addParquet);

    await waitForPmPresence(request, nhsNumbers[0]);

    const blockResp = await BlockParticipant(request, buildBlockPayload(nhsNumbers, input));
    await composeValidators(expectStatus(200))(blockResp);
    await waitForPmBlockedFlag(request, nhsNumbers[0]);

    const delResp = await deleteParticipant(request, buildDeletePayload(nhsNumbers, input));
    await composeValidators(expectStatus(200))(delResp);

    await validateSqlDatabaseFromAPI(request, validations);
  });
});
