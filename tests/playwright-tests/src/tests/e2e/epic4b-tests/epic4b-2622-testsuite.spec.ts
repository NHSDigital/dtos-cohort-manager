import { test, expect, APIRequestContext } from '@playwright/test';
import * as fs from 'fs';
import * as path from 'path';
import { createParquetFromJson } from '../../../parquet/parquet-multiplier';
import { processFileViaStorage, validateSqlDatabaseFromAPI, cleanupDatabaseFromAPI } from '../../steps/steps';
import { fetchApiResponse } from '../../../api/apiHelper';
import { deleteParticipant } from '../../../api/distributionService/bsSelectService';
import { expectStatus } from '../../../api/responseValidators';
import { config } from '../../../config/env';

const PM = 'api/ParticipantManagementDataService';
const CD = 'api/CohortDistributionDataService';
const EX = 'api/ExceptionManagementDataService';

const sleep = (ms:number)=>new Promise(r=>setTimeout(r,ms));

function scenarioDir(tag: string) {
  const here = path.resolve(__dirname);
  return path.resolve(here, '../e2e/testFiles', tag);
}

function readJson(p: string): any {
  return JSON.parse(fs.readFileSync(p, 'utf-8'));
}

function pickActionFile(dir: string, action: 'ADD'|'AMENDED'|'DELETE') {
  const files = fs.readdirSync(dir).filter(f => f.endsWith('.json'));
  const upper = files.map(f => f.toUpperCase());
  const hitIdx = upper.findIndex(u => u.startsWith(action));
  if (hitIdx >= 0) return path.join(dir, files[hitIdx]);
  const containsIdx = upper.findIndex(u => u.includes(action));
  if (containsIdx >= 0) return path.join(dir, files[containsIdx]);
  throw new Error(`No ${action} json found in ${dir}`);
}

function normaliseValidations(raw: any): any[] {
  if (!raw) return [];
  if (Array.isArray(raw)) return raw;
  if (raw.validations) return Array.isArray(raw.validations) ? raw.validations : [{ validations: raw.validations }];
  return [{ validations: raw }];
}

function normaliseEndpoint(ep?: string): string {
  if (!ep) return PM;
  const e = ep.toLowerCase();
  if (e.includes('cohort')) return CD;
  if (e.includes('participantmanagement')) return PM;
  if (e.includes('exception')) return EX;
  return ep.startsWith('api/') ? ep : `api/${ep}`;
}

function fixValidationEndpoints(vs: any[]): any[] {
  return vs.map(x => ({ validations: { ...x.validations, apiEndpoint: normaliseEndpoint(x.validations?.apiEndpoint) } }));
}

function withNhs(records: any[], nhsNumbers: string[]) {
  return records.map((r, i) => ({
    ...r,
    NHSNumber: r?.NHSNumber ?? r?.NhsNumber ?? nhsNumbers[i] ?? nhsNumbers[0],
    NhsNumber: r?.NhsNumber ?? r?.NHSNumber ?? nhsNumbers[i] ?? nhsNumbers[0],
  }));
}

async function createParquetOrThrow(
  nhs: string[], recs: any[], testFilesPath: string, kind: 'ADD'|'AMENDED'='ADD', multiply=false
): Promise<string> {
  const file = await createParquetFromJson(nhs, recs, testFilesPath, kind, multiply);
  if (typeof file !== 'string' || !file.endsWith('.parquet') || !fs.existsSync(file)) {
    throw new Error(`Parquet was not created correctly for ${kind}. Got: ${file}`);
  }
  return file;
}

async function waitForPmRow(request: APIRequestContext, nhs: string) {
  const attempts = Math.max(Number(config.apiRetry) || 20, 20);
  const delay = Math.max(Number(config.apiWaitTime) || 5000, 5000);
  let last = 0;
  for (let i=0;i<attempts;i++) {
    const resp = await fetchApiResponse(PM, request);
    last = resp.status();
    if (last === 200) {
      const body = await resp.json();
      if (Array.isArray(body)) {
        const row = body.find((r: any) => String(r.NHSNumber ?? r.NhsNumber) === String(nhs)) || body[0];
        if (row) return row;
      }
    }
    await sleep(delay);
  }
  throw new Error(`Timed out waiting for ParticipantManagement data for NHS ${nhs}. Last status=${last}`);
}

async function loadScenario(tag: string, action: 'ADD'|'AMENDED'|'DELETE') {
  const dir = scenarioDir(tag);
  const f = pickActionFile(dir, action);
  const raw = readJson(f);

  const inputRecords = Array.isArray(raw.inputParticipantRecord) ? raw.inputParticipantRecord
                        : raw.inputParticipantRecord ? [raw.inputParticipantRecord]
                        : Array.isArray(raw.records) ? raw.records
                        : [raw];

  let nhsNumbers: string[] =
      Array.isArray(raw.nhsNumbers) ? raw.nhsNumbers.map(String)
    : Array.isArray(raw.NhsNumbers) ? raw.NhsNumbers.map(String)
    : [];

  const vArray = fixValidationEndpoints(normaliseValidations(raw.validations));
  if (nhsNumbers.length === 0 && vArray.length) {
    nhsNumbers = vArray
      .map(v => String(v.validations.NHSNumber ?? v.validations.NhsNumber))
      .filter(Boolean);
  }
  if (nhsNumbers.length === 0) throw new Error(`No nhsNumbers found in ${f}`);

  return { dir, nhsNumbers, inputRecords, validations: vArray };
}

function buildDeletePayload(row: any, nhs: string, fallback: any) {
  const NhsNumber = String(row?.NHSNumber ?? row?.NhsNumber ?? nhs);
  const FamilyName = String(row?.FamilyName ?? row?.family_name ?? fallback?.family_name ?? 'TEST');
  let dob = String(row?.DateOfBirth ?? row?.date_of_birth ?? fallback?.date_of_birth ?? '').replace(/[^0-9]/g,'');
  if (dob.length >= 8) dob = `${dob.slice(0,4)}-${dob.slice(4,6)}-${dob.slice(6,8)}`; else dob = '1970-01-01';
  return { NhsNumber, FamilyName, DateOfBirth: dob };
}

async function seedAndProcess(
  request: APIRequestContext,
  nhs: string[], recs: any[], dir: string, action: 'ADD'|'AMENDED'|'DELETE', keepBlockedOnRuntime = true
) {
  // seed as blocked
  const seed = withNhs([{ ...recs[0], BlockedFlag: 1 }], nhs);
  const seedP = await createParquetOrThrow(nhs, seed, dir, 'ADD', false);
  await processFileViaStorage(seedP);

  if (action === 'DELETE') return;

  const runtime = keepBlockedOnRuntime
    ? withNhs(recs.map(r => ({ ...r, BlockedFlag: 1 })), nhs)
    : withNhs(recs, nhs);

  const kind = action === 'AMENDED' ? 'AMENDED' : 'ADD';
  const runP = await createParquetOrThrow(nhs, runtime, dir, kind, false);
  await processFileViaStorage(runP);
}

test.describe('@regression @e2e @epic4b-block-tests (final)', () => {

  test('@DTOSS-7610-01', async ({ request }) => {
    const tag='@DTOSS-7610-01';
    const { dir, nhsNumbers, inputRecords, validations } = await loadScenario(tag, 'ADD');
    await cleanupDatabaseFromAPI(request, nhsNumbers);

    await seedAndProcess(request, nhsNumbers, inputRecords, dir, 'ADD', true);

    const row = await waitForPmRow(request, nhsNumbers[0]);
    expect(row.BlockedFlag).toBe(1);

    await validateSqlDatabaseFromAPI(request, validations);
  });

  test('@DTOSS-7614-01', async ({ request }) => {
    const tag='@DTOSS-7614-01';
    const { dir, nhsNumbers, inputRecords, validations } = await loadScenario(tag, 'AMENDED');
    await cleanupDatabaseFromAPI(request, nhsNumbers);

    await seedAndProcess(request, nhsNumbers, inputRecords, dir, 'AMENDED', true);

    await validateSqlDatabaseFromAPI(request, validations);
  });

  test('@DTOSS-7615-01', async ({ request }) => {
    const tag='@DTOSS-7615-01';
    const { dir, nhsNumbers, inputRecords, validations } = await loadScenario(tag, 'DELETE');
    await cleanupDatabaseFromAPI(request, nhsNumbers);

    await seedAndProcess(request, nhsNumbers, inputRecords, dir, 'DELETE', true);

    const row = await waitForPmRow(request, nhsNumbers[0]);
    const del = buildDeletePayload(row, nhsNumbers[0], inputRecords[0]);
    const resp = await deleteParticipant(request, del);
    await expectStatus(200)(resp);

    await validateSqlDatabaseFromAPI(request, validations);
  });

  test('@DTOSS-7616-01', async ({ request }) => {
    const tag='@DTOSS-7616-01';
    const { dir, nhsNumbers, inputRecords, validations } = await loadScenario(tag, 'ADD');
    await cleanupDatabaseFromAPI(request, nhsNumbers);

    await seedAndProcess(request, nhsNumbers, inputRecords, dir, 'ADD', true);

    await validateSqlDatabaseFromAPI(request, validations);
  });

  test('@DTOSS-7660-01', async ({ request }) => {
    const tag='@DTOSS-7660-01';
    const { dir, nhsNumbers, inputRecords, validations } = await loadScenario(tag, 'AMENDED');
    await cleanupDatabaseFromAPI(request, nhsNumbers);

    await seedAndProcess(request, nhsNumbers, inputRecords, dir, 'AMENDED', true);

    await validateSqlDatabaseFromAPI(request, validations);
  });

  test('@DTOSS-7661-01', async ({ request }) => {
    const tag='@DTOSS-7661-01';
    const { dir, nhsNumbers, inputRecords, validations } = await loadScenario(tag, 'DELETE');
    await cleanupDatabaseFromAPI(request, nhsNumbers);

    await seedAndProcess(request, nhsNumbers, inputRecords, dir, 'DELETE', true);

    const row = await waitForPmRow(request, nhsNumbers[0]);
    const del = buildDeletePayload(row, nhsNumbers[0], inputRecords[0]);
    const resp = await deleteParticipant(request, del);
    await expectStatus(200)(resp);

    await validateSqlDatabaseFromAPI(request, validations);
  });

  test('@DTOSS-7663-01', async ({ request }) => {
    const tag='@DTOSS-7663-01';
    const { dir, nhsNumbers, inputRecords, validations } = await loadScenario(tag, 'ADD');
    await cleanupDatabaseFromAPI(request, nhsNumbers);

    // seed blocked + ineligible, then process original input (do not force block on runtime)
    const seed = withNhs([{ ...inputRecords[0], BlockedFlag: 1, EligibilityFlag: '0' }], nhsNumbers);
    const seedP = await createParquetOrThrow(nhsNumbers, seed, dir, 'ADD', false);
    await processFileViaStorage(seedP);

    const runP = await createParquetOrThrow(nhsNumbers, withNhs(inputRecords, nhsNumbers), dir, 'ADD', false);
    await processFileViaStorage(runP);

    await validateSqlDatabaseFromAPI(request, validations);
  });

  test('@DTOSS-7664-01', async ({ request }) => {
    const tag='@DTOSS-7664-01';
    const { dir, nhsNumbers, inputRecords, validations } = await loadScenario(tag, 'ADD');
    await cleanupDatabaseFromAPI(request, nhsNumbers);

    await seedAndProcess(request, nhsNumbers, inputRecords, dir, 'ADD', true);

    await validateSqlDatabaseFromAPI(request, validations);
  });

  test('@DTOSS-7665-01', async ({ request }) => {
    const tag='@DTOSS-7665-01';
    const { dir, nhsNumbers, inputRecords, validations } = await loadScenario(tag, 'AMENDED');
    await cleanupDatabaseFromAPI(request, nhsNumbers);

    await seedAndProcess(request, nhsNumbers, inputRecords, dir, 'AMENDED', true);

    await validateSqlDatabaseFromAPI(request, validations);
  });

  test('@DTOSS-7666-01', async ({ request }) => {
    const tag='@DTOSS-7666-01';
    const { dir, nhsNumbers, inputRecords, validations } = await loadScenario(tag, 'DELETE');
    await cleanupDatabaseFromAPI(request, nhsNumbers);

    await seedAndProcess(request, nhsNumbers, inputRecords, dir, 'DELETE', true);

    const row = await waitForPmRow(request, nhsNumbers[0]);
    const del = buildDeletePayload(row, nhsNumbers[0], inputRecords[0]);
    const resp = await deleteParticipant(request, del);
    await expectStatus(200)(resp);

    await validateSqlDatabaseFromAPI(request, validations);
  });

});
