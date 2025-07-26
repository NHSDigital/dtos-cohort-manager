import { test, expect, APIRequestContext } from '@playwright/test';
import * as fs from 'fs';
import { createParquetFromJson } from '../../../parquet/parquet-multiplier';
import {
  getTestData,
  processFileViaStorage,
  validateSqlDatabaseFromAPI,
  cleanupDatabaseFromAPI
} from '../../steps/steps';
import { fetchApiResponse } from '../../../api/apiHelper';
import { deleteParticipant } from '../../../api/distributionService/bsSelectService';
import { config } from '../../../config/env';

const PM_ENDPOINT = 'api/ParticipantManagementDataService';

const sleep = (ms: number) => new Promise(res => setTimeout(res, ms));

async function getFirstPmRowWithRetry(request: APIRequestContext) {
  const attempts = Number(config.apiRetry) || 8;
  const waitMs = Number(config.apiWaitTime) || 2000;
  let lastStatus = 0;

  for (let i = 0; i < attempts; i++) {
    const resp = await fetchApiResponse(PM_ENDPOINT, request);
    lastStatus = resp.status();
    if (lastStatus === 200) {
      const body = await resp.json();
      if (Array.isArray(body) && body.length > 0) {
        return body[0];
      }
    }
    await sleep(waitMs);
  }
  throw new Error(`Timed out waiting for ParticipantManagement data. Last status=${lastStatus}`);
}

/** Guard around parquet creation so we never proceed with an invalid file path */
async function createParquetOrThrow(
  nhsNumbers: string[],
  records: any[],
  testFilesPath: string,
  recordType: 'ADD'|'AMENDED' = 'ADD',
  multiply = false
): Promise<string> {
  const p = await createParquetFromJson(nhsNumbers, records, testFilesPath, recordType, multiply);
  if (typeof p !== 'string' || !p.endsWith('.parquet') || !fs.existsSync(p)) {
    throw new Error(`Parquet was not created correctly for ${recordType}. Got: ${p}`);
  }
  return p;
}

/** Ensure Nhs/NHS numbers are present on each record */
function withNhsNumbers(records: any[], nhsNumbers: string[]) {
  return records.map((rec, i) => ({
    ...rec,
    NHSNumber: rec?.NHSNumber ?? rec?.NhsNumber ?? nhsNumbers[i] ?? nhsNumbers[0],
    NhsNumber: rec?.NhsNumber ?? rec?.NHSNumber ?? nhsNumbers[i] ?? nhsNumbers[0],
  }));
}

/** Build a Delete payload from a PM row + scenario defaults */
function buildDeletePayload(row: any, nhs: string, fallback: any) {
  const ns = String(row?.NHSNumber ?? row?.NhsNumber ?? nhs);
  const fam = String(row?.FamilyName ?? row?.family_name ?? fallback?.family_name ?? 'TEST');
  // Normalise to YYYY-MM-DD
  let dobSource = String(row?.DateOfBirth ?? row?.date_of_birth ?? fallback?.date_of_birth ?? '');
  dobSource = dobSource.replace(/[^0-9]/g, '');
  if (dobSource.length >= 8) {
    dobSource = `${dobSource.slice(0,4)}-${dobSource.slice(4,6)}-${dobSource.slice(6,8)}`;
  } else {
    dobSource = '1970-01-01';
  }
  return { NhsNumber: ns, FamilyName: fam, DateOfBirth: dobSource };
}

async function seedBlockedThenProcess(
  request: APIRequestContext,
  nhsNumbers: string[],
  inputRecords: any[],
  testFilesPath: string,
  action: 'ADD'|'AMENDED'|'DELETE',
  keepBlockedOnRuntime: boolean
) {
  // Seed as blocked
  const seed = withNhsNumbers([{ ...inputRecords[0], BlockedFlag: 1 }], nhsNumbers);
  const seedParquet = await createParquetOrThrow(nhsNumbers, seed, testFilesPath, 'ADD', false);
  await processFileViaStorage(seedParquet);

  if (action === 'DELETE') return;

  const runtime = keepBlockedOnRuntime
    ? withNhsNumbers(inputRecords.map(r => ({ ...r, BlockedFlag: 1 })), nhsNumbers)
    : withNhsNumbers(inputRecords, nhsNumbers);

  const recordType = (action === 'AMENDED') ? 'AMENDED' : 'ADD';
  const runtimeParquet = await createParquetOrThrow(nhsNumbers, runtime, testFilesPath, recordType, false);
  await processFileViaStorage(runtimeParquet);
}

test.describe('@regression @e2e @epic4b-block-tests (no-helper-changes, v2)', () => {

  // 7610 — ADD: blocked participant not processed to cohort
  test('@DTOSS-7610-01', async ({ request }, testInfo) => {
    const [validations, nhsNumbers, _parquet, inputParticipantRecordRaw, testFilesPath] =
      await getTestData(testInfo.title, 'ADD');
    const inputRecords = Array.isArray(inputParticipantRecordRaw) ? (inputParticipantRecordRaw as any[]) : [inputParticipantRecordRaw as any];
    await cleanupDatabaseFromAPI(request, nhsNumbers);

    await seedBlockedThenProcess(request, nhsNumbers, inputRecords, testFilesPath!, 'ADD', true);

    const row = await getFirstPmRowWithRetry(request);
    expect(row?.BlockedFlag).toBe(1);

    await validateSqlDatabaseFromAPI(request, Array.isArray(validations) ? validations : [validations]);
  });

  // 7614 — AMENDED: blocked participant not processed to cohort (Exception expected)
  test('@DTOSS-7614-01', async ({ request }, testInfo) => {
    const [validations, nhsNumbers, _parquet, inputParticipantRecordRaw, testFilesPath] =
      await getTestData(testInfo.title, 'AMENDED');
    const inputRecords = Array.isArray(inputParticipantRecordRaw) ? (inputParticipantRecordRaw as any[]) : [inputParticipantRecordRaw as any];
    await cleanupDatabaseFromAPI(request, nhsNumbers);

    await seedBlockedThenProcess(request, nhsNumbers, inputRecords, testFilesPath!, 'AMENDED', true);
    await validateSqlDatabaseFromAPI(request, Array.isArray(validations) ? validations : [validations]);
  });

  // 7615 — DELETE: blocked participant deletion not processed to cohort
  test('@DTOSS-7615-01', async ({ request }, testInfo) => {
    const [validations, nhsNumbers, _parquet, inputParticipantRecordRaw, testFilesPath] =
      await getTestData(testInfo.title, 'DELETE');
    const inputRecords = Array.isArray(inputParticipantRecordRaw) ? (inputParticipantRecordRaw as any[]) : [inputParticipantRecordRaw as any];
    await cleanupDatabaseFromAPI(request, nhsNumbers);

    await seedBlockedThenProcess(request, nhsNumbers, inputRecords, testFilesPath!, 'DELETE', true);

    const row = await getFirstPmRowWithRetry(request);
    const payload = buildDeletePayload(row, nhsNumbers[0], inputRecords[0]);
    const resp = await deleteParticipant(request, payload);
    expect(resp.status === 200).toBeTruthy();

    await validateSqlDatabaseFromAPI(request, Array.isArray(validations) ? validations : [validations]);
  });

  // 7616 — ADD: no NBO exception for blocked participant
  test('@DTOSS-7616-01', async ({ request }, testInfo) => {
    const [validations, nhsNumbers, _parquet, inputParticipantRecordRaw, testFilesPath] =
      await getTestData(testInfo.title, 'ADD');
    const inputRecords = Array.isArray(inputParticipantRecordRaw) ? (inputParticipantRecordRaw as any[]) : [inputParticipantRecordRaw as any];
    await cleanupDatabaseFromAPI(request, nhsNumbers);

    await seedBlockedThenProcess(request, nhsNumbers, inputRecords, testFilesPath!, 'ADD', true);
    await validateSqlDatabaseFromAPI(request, Array.isArray(validations) ? validations : [validations]);
  });

  // 7660 — AMENDED: no NBO exception for blocked participant
  test('@DTOSS-7660-01', async ({ request }, testInfo) => {
    const [validations, nhsNumbers, _parquet, inputParticipantRecordRaw, testFilesPath] =
      await getTestData(testInfo.title, 'AMENDED');
    const inputRecords = Array.isArray(inputParticipantRecordRaw) ? (inputParticipantRecordRaw as any[]) : [inputParticipantRecordRaw as any];
    await cleanupDatabaseFromAPI(request, nhsNumbers);

    await seedBlockedThenProcess(request, nhsNumbers, inputRecords, testFilesPath!, 'AMENDED', true);
    await validateSqlDatabaseFromAPI(request, Array.isArray(validations) ? validations : [validations]);
  });

  // 7661 — DELETE: no NBO exception for blocked participant
  test('@DTOSS-7661-01', async ({ request }, testInfo) => {
    const [validations, nhsNumbers, _parquet, inputParticipantRecordRaw, testFilesPath] =
      await getTestData(testInfo.title, 'DELETE');
    const inputRecords = Array.isArray(inputParticipantRecordRaw) ? (inputParticipantRecordRaw as any[]) : [inputParticipantRecordRaw as any];
    await cleanupDatabaseFromAPI(request, nhsNumbers);

    await seedBlockedThenProcess(request, nhsNumbers, inputRecords, testFilesPath!, 'DELETE', true);

    const row = await getFirstPmRowWithRetry(request);
    const payload = buildDeletePayload(row, nhsNumbers[0], inputRecords[0]);
    const resp = await deleteParticipant(request, payload);
    expect(resp.status === 200).toBeTruthy();

    await validateSqlDatabaseFromAPI(request, Array.isArray(validations) ? validations : [validations]);
  });

  // 7663 — ADD: blocked ineligible participant becomes eligible (no NBO)
  test('@DTOSS-7663-01', async ({ request }, testInfo) => {
    const [validations, nhsNumbers, _parquet, inputParticipantRecordRaw, testFilesPath] =
      await getTestData(testInfo.title, 'ADD');
    const inputRecords = Array.isArray(inputParticipantRecordRaw) ? (inputParticipantRecordRaw as any[]) : [inputParticipantRecordRaw as any];
    await cleanupDatabaseFromAPI(request, nhsNumbers);

    // Seed blocked + ineligible, then process original input (do not force BlockedFlag on runtime)
    const seed = withNhsNumbers([{ ...inputRecords[0], BlockedFlag: 1, EligibilityFlag: "0" }], nhsNumbers);
    const seedParquet = await createParquetOrThrow(nhsNumbers, seed, testFilesPath!, 'ADD', false);
    await processFileViaStorage(seedParquet);

    const runtimeParquet = await createParquetOrThrow(nhsNumbers, withNhsNumbers(inputRecords, nhsNumbers), testFilesPath!, 'ADD', false);
    await processFileViaStorage(runtimeParquet);

    await validateSqlDatabaseFromAPI(request, Array.isArray(validations) ? validations : [validations]);
  });

  // 7664 — ADD: audit logs updated for blocked participant
  test('@DTOSS-7664-01', async ({ request }, testInfo) => {
    const [validations, nhsNumbers, _parquet, inputParticipantRecordRaw, testFilesPath] =
      await getTestData(testInfo.title, 'ADD');
    const inputRecords = Array.isArray(inputParticipantRecordRaw) ? (inputParticipantRecordRaw as any[]) : [inputParticipantRecordRaw as any];
    await cleanupDatabaseFromAPI(request, nhsNumbers);

    await seedBlockedThenProcess(request, nhsNumbers, inputRecords, testFilesPath!, 'ADD', true);
    await validateSqlDatabaseFromAPI(request, Array.isArray(validations) ? validations : [validations]);
  });

  // 7665 — AMENDED: audit logs updated for blocked participant
  test('@DTOSS-7665-01', async ({ request }, testInfo) => {
    const [validations, nhsNumbers, _parquet, inputParticipantRecordRaw, testFilesPath] =
      await getTestData(testInfo.title, 'AMENDED');
    const inputRecords = Array.isArray(inputParticipantRecordRaw) ? (inputParticipantRecordRaw as any[]) : [inputParticipantRecordRaw as any];
    await cleanupDatabaseFromAPI(request, nhsNumbers);

    await seedBlockedThenProcess(request, nhsNumbers, inputRecords, testFilesPath!, 'AMENDED', true);
    await validateSqlDatabaseFromAPI(request, Array.isArray(validations) ? validations : [validations]);
  });

  // 7666 — DELETE: audit logs updated for blocked participant
  test('@DTOSS-7666-01', async ({ request }, testInfo) => {
    const [validations, nhsNumbers, _parquet, inputParticipantRecordRaw, testFilesPath] =
      await getTestData(testInfo.title, 'DELETE');
    const inputRecords = Array.isArray(inputParticipantRecordRaw) ? (inputParticipantRecordRaw as any[]) : [inputParticipantRecordRaw as any];
    await cleanupDatabaseFromAPI(request, nhsNumbers);

    await seedBlockedThenProcess(request, nhsNumbers, inputRecords, testFilesPath!, 'DELETE', true);

    const row = await getFirstPmRowWithRetry(request);
    const payload = buildDeletePayload(row, nhsNumbers[0], inputRecords[0]);
    const resp = await deleteParticipant(request, payload);
    expect(resp.status === 200).toBeTruthy();

    await validateSqlDatabaseFromAPI(request, Array.isArray(validations) ? validations : [validations]);
  });

});
