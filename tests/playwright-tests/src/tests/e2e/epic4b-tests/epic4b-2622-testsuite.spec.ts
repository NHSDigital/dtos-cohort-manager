import { test, APIRequestContext } from '@playwright/test';
import { createParquetFromJson } from '../../../parquet/parquet-multiplier';
import {
  getApiTestData,
  processFileViaStorage,
  cleanupDatabaseFromAPI,
  validateSqlDatabaseFromAPI,
} from '../../steps/steps';
import { composeValidators, expectStatus } from '../../../api/responseValidators';
import { BlockParticipant, deleteParticipant } from '../../../api/distributionService/bsSelectService';
import { fetchApiResponse } from '../../../api/apiHelper';

function asArray<T>(x: T | T[] | undefined): T[] { return !x ? [] : (Array.isArray(x) ? x : [x]); }
const isObj = (v: unknown) => v !== null && typeof v === 'object' && !Array.isArray(v);
const looksLikeInput = (v: unknown) =>
  Array.isArray(v) && v.length > 0 && isObj(v[0]) && ('family_name' in (v[0] as any) || 'date_of_birth' in (v[0] as any));
const looksLikeNhs = (v: unknown) =>
  Array.isArray(v) && v.length > 0 && v.every(x => typeof x === 'string' || typeof x === 'number');
const looksLikeDir = (v: unknown) => typeof v === 'string' && v.includes('/testFiles/@DTOSS-');

async function loadData(testTitle: string) {
  const tuple: any[] = await getApiTestData(testTitle);
  let validations: any[] = [];
  let inputParticipantRecord: any[] = [];
  let nhsNumbers: string[] = [];
  let testFilesPath = '';
  for (const part of tuple) {
    if (!validations.length && Array.isArray(part) && !looksLikeInput(part) && !looksLikeNhs(part)) {
      validations = asArray(part);
      continue;
    }
    if (!inputParticipantRecord.length && looksLikeInput(part)) {
      inputParticipantRecord = asArray(part);
      continue;
    }
    if (!nhsNumbers.length && looksLikeNhs(part)) {
      nhsNumbers = (part as (string|number)[]).map(String);
      continue;
    }
    if (!testFilesPath && looksLikeDir(part)) {
      testFilesPath = String(part);
      continue;
    }
  }
  if (!inputParticipantRecord.length || !nhsNumbers.length || !testFilesPath) {
    throw new Error(`Data mapping error for ${testTitle}.
Resolved:
  validations: ${Array.isArray(validations) ? validations.length : 'N/A'}
  input[0]: ${JSON.stringify(inputParticipantRecord[0] ?? null)}
  nhsNumbers: ${JSON.stringify(nhsNumbers)}
  testFilesPath: ${testFilesPath || 'N/A'}
Raw tuple types: ${JSON.stringify(tuple.map(t => (Array.isArray(t) ? `array(len=${t.length})` : typeof t)))}`);
  }
  return { validations, inputParticipantRecord, nhsNumbers, testFilesPath };
}

function buildBlockPayload(nhsNumbers: string[], input: any[]) {
  const r0 = input[0] ?? {};
  return { NhsNumber: nhsNumbers[0], FamilyName: r0.family_name, DateOfBirth: r0.date_of_birth };
}
function buildDeletePayload(nhsNumbers: string[], input: any[]) {
  const r0 = input[0] ?? {};
  const dob = String(r0.date_of_birth ?? '');
  const iso = /^\d{8}$/.test(dob) ? `${dob.slice(0,4)}-${dob.slice(4,6)}-${dob.slice(6,8)}` : dob;
  return { NhsNumber: nhsNumbers[0], FamilyName: r0.family_name, DateOfBirth: iso };
}

async function waitForPmPresence(request: APIRequestContext, nhs: string, attempts = 30, waitMs = 5000) {
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

async function waitForPmBlockedFlag(request: APIRequestContext, nhs: string, attempts = 24, waitMs = 5000) {
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

// --- Tests ---
test.describe.serial('@regression @e2e @epic4b-2622', () => {

  async function runAddOrAmendedFlow(request: APIRequestContext, testInfo: any, recordType: 'ADD'|'AMENDED') {
    const { validations, inputParticipantRecord, nhsNumbers, testFilesPath } = await loadData(testInfo.title);

    await cleanupDatabaseFromAPI(request, nhsNumbers);

    // Build parquet with actual input and correct dir
    const parquet = await createParquetFromJson(nhsNumbers, inputParticipantRecord, testFilesPath, recordType, false);
    if (typeof parquet !== 'string' || !parquet.endsWith('.parquet')) {
      throw new Error(`Unexpected parquet path from createParquetFromJson: ${parquet}`);
    }
    await processFileViaStorage(parquet);

    await waitForPmPresence(request, nhsNumbers[0]);

    const blockResp = await BlockParticipant(request, buildBlockPayload(nhsNumbers, inputParticipantRecord));
    await composeValidators(expectStatus(200))(blockResp);

    await waitForPmBlockedFlag(request, nhsNumbers[0]);

    await validateSqlDatabaseFromAPI(request, validations);
  }

  async function runDeleteFlow(request: APIRequestContext, testInfo: any) {
    const { validations, inputParticipantRecord, nhsNumbers, testFilesPath } = await loadData(testInfo.title);

    await cleanupDatabaseFromAPI(request, nhsNumbers);

    const addParquet = await createParquetFromJson(nhsNumbers, inputParticipantRecord, testFilesPath, 'ADD', false);
    if (typeof addParquet !== 'string' || !addParquet.endsWith('.parquet')) {
      throw new Error(`Unexpected parquet path from createParquetFromJson: ${addParquet}`);
    }
    await processFileViaStorage(addParquet);

    await waitForPmPresence(request, nhsNumbers[0]);

    const blockResp = await BlockParticipant(request, buildBlockPayload(nhsNumbers, inputParticipantRecord));
    await composeValidators(expectStatus(200))(blockResp);

    await waitForPmBlockedFlag(request, nhsNumbers[0]);

    const delResp = await deleteParticipant(request, buildDeletePayload(nhsNumbers, inputParticipantRecord));
    await composeValidators(expectStatus(200))(delResp);

    await validateSqlDatabaseFromAPI(request, validations);
  }

  test('@DTOSS-7610-01 ADD: blocked not processed to cohort', async ({ request }, testInfo) => {
    await runAddOrAmendedFlow(request, testInfo, 'ADD');
  });

  test('@DTOSS-7614-01 AMENDED: blocked not processed to cohort', async ({ request }, testInfo) => {
    await runAddOrAmendedFlow(request, testInfo, 'AMENDED');
  });

  test('@DTOSS-7615-01 DELETE: blocked deletion not processed to cohort', async ({ request }, testInfo) => {
    await runDeleteFlow(request, testInfo);
  });

  test('@DTOSS-7616-01 ADD: no NBO exception for blocked participant', async ({ request }, testInfo) => {
    await runAddOrAmendedFlow(request, testInfo, 'ADD');
  });

  test('@DTOSS-7660-01 AMENDED: no NBO exception for blocked participant', async ({ request }, testInfo) => {
    await runAddOrAmendedFlow(request, testInfo, 'AMENDED');
  });

  test('@DTOSS-7661-01 DELETE: no NBO exception for blocked participant', async ({ request }, testInfo) => {
    await runDeleteFlow(request, testInfo);
  });

  test('@DTOSS-7663-01 ADD: blocked ineligible becomes eligible (no NBO)', async ({ request }, testInfo) => {
    const { validations, inputParticipantRecord, nhsNumbers, testFilesPath } = await loadData(testInfo.title);

    await cleanupDatabaseFromAPI(request, nhsNumbers);

    // Seed ineligible first
    const seed = [{ ...inputParticipantRecord[0], EligibilityFlag: '0' }];
    const seedParquet = await createParquetFromJson(nhsNumbers, seed, testFilesPath, 'ADD', false);
    if (typeof seedParquet !== 'string' || !seedParquet.endsWith('.parquet')) {
      throw new Error(`Unexpected parquet path from createParquetFromJson (seed): ${seedParquet}`);
    }
    await processFileViaStorage(seedParquet);

    await waitForPmPresence(request, nhsNumbers[0]);

    const blockResp = await BlockParticipant(request, buildBlockPayload(nhsNumbers, seed));
    await composeValidators(expectStatus(200))(blockResp);
    await waitForPmBlockedFlag(request, nhsNumbers[0]);

    const runParquet = await createParquetFromJson(nhsNumbers, inputParticipantRecord, testFilesPath, 'ADD', false);
    if (typeof runParquet !== 'string' || !runParquet.endsWith('.parquet')) {
      throw new Error(`Unexpected parquet path from createParquetFromJson (run): ${runParquet}`);
    }
    await processFileViaStorage(runParquet);

    await validateSqlDatabaseFromAPI(request, validations);
  });

  test('@DTOSS-7664-01 ADD: audit logs updated for blocked participant', async ({ request }, testInfo) => {
    await runAddOrAmendedFlow(request, testInfo, 'ADD');
  });

  test('@DTOSS-7665-01 AMENDED: audit logs updated for blocked participant', async ({ request }, testInfo) => {
    await runAddOrAmendedFlow(request, testInfo, 'AMENDED');
  });

  test('@DTOSS-7666-01 DELETE: audit logs updated for blocked participant', async ({ request }, testInfo) => {
    await runDeleteFlow(request, testInfo);
  });
});
