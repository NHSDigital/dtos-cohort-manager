import { test } from '@playwright/test';
import { createParquetFromJson } from '../../../parquet/parquet-multiplier';
import {
  getApiTestData,
  processFileViaStorage,
  cleanupDatabaseFromAPI,
  validateSqlDatabaseFromAPI,
} from '../../steps/steps';
import { composeValidators, expectStatus } from '../../../api/responseValidators';
import { BlockParticipant, deleteParticipant } from '../../../api/distributionService/bsSelectService';

// helpers
const asArray = <T,>(x: T | T[] | undefined): T[] => (!x ? [] : Array.isArray(x) ? x : [x]);

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

test.describe.serial('@regression @e2e @epic4b-2622', () => {
  async function runAddOrAmendedFlow(testInfo: any) {
    const [validations, inputParticipantRecord, nhsNumbers, testFilesPath] =
      await getApiTestData(testInfo.title);

    const input = asArray(inputParticipantRecord);

    await cleanupDatabaseFromAPI(testInfo.request, nhsNumbers);

    // Create & ingest parquet
    const parquet = await createParquetFromJson(nhsNumbers, input, testFilesPath);
    await processFileViaStorage(parquet);

    // Request a block
    await composeValidators(expectStatus(200))(await BlockParticipant(testInfo.request, buildBlockPayload(nhsNumbers, input)));

    // Using shared validator to drive the polling/retries and assertions
    await validateSqlDatabaseFromAPI(testInfo.request, validations);
  }

  async function runDeleteFlow(testInfo: any) {
    const [validations, inputParticipantRecord, nhsNumbers, testFilesPath] =
      await getApiTestData(testInfo.title);

    const input = asArray(inputParticipantRecord);

    await cleanupDatabaseFromAPI(testInfo.request, nhsNumbers);

    // Seed via ADD then block
    const addParquet = await createParquetFromJson(nhsNumbers, input, testFilesPath);
    await processFileViaStorage(addParquet);

    await composeValidators(expectStatus(200))(await BlockParticipant(testInfo.request, buildBlockPayload(nhsNumbers, input)));

    // DELETE request
    await composeValidators(expectStatus(200))(
      await deleteParticipant(testInfo.request, buildDeletePayload(nhsNumbers, input))
    );

    await validateSqlDatabaseFromAPI(testInfo.request, validations);
  }

  // 7610 — ADD: blocked participant not processed to cohort
  test('@DTOSS-7610-01 ADD: blocked not processed to cohort', async ({ request }, testInfo) => {
    // expose request on testInfo for our helpers above
    (testInfo as any).request = request;
    await runAddOrAmendedFlow(testInfo);
  });

  // 7614 — AMENDED: blocked participant not processed to cohort
  test('@DTOSS-7614-01 AMENDED: blocked not processed to cohort', async ({ request }, testInfo) => {
    (testInfo as any).request = request;
    await runAddOrAmendedFlow(testInfo);
  });

  // 7615 — DELETE: blocked participant deletion not processed to cohort
  test('@DTOSS-7615-01 DELETE: blocked deletion not processed to cohort', async ({ request }, testInfo) => {
    (testInfo as any).request = request;
    await runDeleteFlow(testInfo);
  });

  // 7616 — ADD: no NBO exception for blocked participant
  test('@DTOSS-7616-01 ADD: no NBO exception for blocked participant', async ({ request }, testInfo) => {
    (testInfo as any).request = request;
    await runAddOrAmendedFlow(testInfo);
  });

  // 7660 — AMENDED: no NBO exception for blocked participant
  test('@DTOSS-7660-01 AMENDED: no NBO exception for blocked participant', async ({ request }, testInfo) => {
    (testInfo as any).request = request;
    await runAddOrAmendedFlow(testInfo);
  });

  // 7661 — DELETE: no NBO exception for blocked participant
  test('@DTOSS-7661-01 DELETE: no NBO exception for blocked participant', async ({ request }, testInfo) => {
    (testInfo as any).request = request;
    await runDeleteFlow(testInfo);
  });

  // 7663 — ADD: blocked ineligible participant becomes eligible (no NBO)
  test('@DTOSS-7663-01 ADD: blocked ineligible becomes eligible (no NBO)', async ({ request }, testInfo) => {
    const [validations, inputParticipantRecord, nhsNumbers, testFilesPath] =
      await getApiTestData(testInfo.title);
    const input = asArray(inputParticipantRecord);

    await cleanupDatabaseFromAPI(request, nhsNumbers);

    // Seed INELIGIBLE first, then block, then process original input
    const seed = [{ ...input[0], EligibilityFlag: '0' }];
    const seedParquet = await createParquetFromJson(nhsNumbers, seed, testFilesPath);
    await processFileViaStorage(seedParquet);

    await composeValidators(expectStatus(200))(await BlockParticipant(request, buildBlockPayload(nhsNumbers, seed)));

    const runParquet = await createParquetFromJson(nhsNumbers, input, testFilesPath);
    await processFileViaStorage(runParquet);

    await validateSqlDatabaseFromAPI(request, validations);
  });

  // 7664 — ADD: audit logs updated for blocked participant
  test('@DTOSS-7664-01 ADD: audit logs updated for blocked participant', async ({ request }, testInfo) => {
    (testInfo as any).request = request;
    await runAddOrAmendedFlow(testInfo);
  });

  // 7665 — AMENDED: audit logs updated for blocked participant
  test('@DTOSS-7665-01 AMENDED: audit logs updated for blocked participant', async ({ request }, testInfo) => {
    (testInfo as any).request = request;
    await runAddOrAmendedFlow(testInfo);
  });

  // 7666 — DELETE: audit logs updated for blocked participant
  test('@DTOSS-7666-01 DELETE: audit logs updated for blocked participant', async ({ request }, testInfo) => {
    (testInfo as any).request = request;
    await runDeleteFlow(testInfo);
  });
});
