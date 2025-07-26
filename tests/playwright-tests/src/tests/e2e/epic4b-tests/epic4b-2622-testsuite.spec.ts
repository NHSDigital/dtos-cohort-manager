import { test, expect, APIRequestContext } from '@playwright/test';
import { createParquetFromJson } from '../../../parquet/parquet-multiplier';
import {
  getApiTestData,
  processFileViaStorage,
  cleanupDatabaseFromAPI,
  validateSqlDatabaseFromAPI,
} from '../../steps/steps';
import { composeValidators, expectStatus } from '../../../api/responseValidators';
import { deleteParticipant } from '../../../api/distributionService/bsSelectService';

function asArray<T>(x: T | T[] | undefined): T[] {
  if (!x) return [];
  return Array.isArray(x) ? x : [x];
}

function withNhs(records: any[], nhsNumbers: string[]) {
  return records.map((r, i) => ({
    ...r,
    NHSNumber: r?.NHSNumber ?? r?.NhsNumber ?? nhsNumbers[i] ?? nhsNumbers[0],
    NhsNumber: r?.NhsNumber ?? r?.NHSNumber ?? nhsNumbers[i] ?? nhsNumbers[0],
  }));
}

function buildDeletePayloadFromInput(nhsNumbers: string[], inputRecords: any[]) {
  const r0 = inputRecords[0] ?? {};
  const dobRaw = String(r0.date_of_birth ?? '');
  const dob = `${dobRaw.slice(0, 4)}-${dobRaw.slice(4, 6)}-${dobRaw.slice(6, 8)}`;
  return {
    NhsNumber: String(nhsNumbers[0]),
    FamilyName: String(r0.family_name ?? 'TEST'),
    DateOfBirth: dob,
  };
}

async function seedBlockedThenProcess(
  request: APIRequestContext,
  nhsNumbers: string[],
  inputRecords: any[],
  testFilesPath: string,
  action: 'ADD' | 'AMENDED' | 'DELETE',
  keepBlockedOnRuntime: boolean
) {
  // 1) seed as blocked
  const seed = withNhs([{ ...inputRecords[0], BlockedFlag: 1 }], nhsNumbers);
  const seedParquet = await createParquetFromJson(nhsNumbers, seed, testFilesPath);
  await processFileViaStorage(seedParquet);

  // 2) runtime (not for DELETE)
  if (action === 'DELETE') return;

  const runtime = keepBlockedOnRuntime
    ? withNhs(inputRecords.map(r => ({ ...r, BlockedFlag: 1 })), nhsNumbers)
    : withNhs(inputRecords, nhsNumbers);

  const runParquet = await createParquetFromJson(nhsNumbers, runtime, testFilesPath);
  await processFileViaStorage(runParquet);
}

test.describe.serial('@regression @e2e @epic4b-2622', () => {
  // 7610 — ADD: blocked participant not processed to cohort
  test('@DTOSS-7610-01 ADD: blocked not processed to cohort', async ({ request }, testInfo) => {
    const [validations, inputParticipantRecord, nhsNumbers, testFilesPath] =
      await getApiTestData(testInfo.title, 'ADD');

    await cleanupDatabaseFromAPI(request, nhsNumbers);

    await test.step('When a blocked participant ADD is seeded & processed', async () => {
      await seedBlockedThenProcess(request, nhsNumbers, asArray(inputParticipantRecord), testFilesPath!, 'ADD', true);
    });

    await test.step('Then DB/API validations should pass', async () => {
      await validateSqlDatabaseFromAPI(request, validations);
    });
  });

  // 7614 — AMENDED: blocked participant not processed to cohort (exception expected)
  test('@DTOSS-7614-01 AMENDED: blocked not processed to cohort', async ({ request }, testInfo) => {
    const [validations, inputParticipantRecord, nhsNumbers, testFilesPath] =
      await getApiTestData(testInfo.title, 'AMENDED');

    await cleanupDatabaseFromAPI(request, nhsNumbers);

    await test.step('When a blocked participant AMENDED is seeded & processed', async () => {
      await seedBlockedThenProcess(request, nhsNumbers, asArray(inputParticipantRecord), testFilesPath!, 'AMENDED', true);
    });

    await test.step('Then DB/API validations should pass', async () => {
      await validateSqlDatabaseFromAPI(request, validations);
    });
  });

  // 7615 — DELETE: blocked participant deletion not processed to cohort
  test('@DTOSS-7615-01 DELETE: blocked deletion not processed to cohort', async ({ request }, testInfo) => {
    const [validations, inputParticipantRecord, nhsNumbers, testFilesPath] =
      await getApiTestData(testInfo.title, 'DELETE');

    await cleanupDatabaseFromAPI(request, nhsNumbers);

    await test.step('Given a blocked participant record exists', async () => {
      await seedBlockedThenProcess(request, nhsNumbers, asArray(inputParticipantRecord), testFilesPath!, 'DELETE', true);
    });

    await test.step('When DELETE is requested via API', async () => {
      const payload = buildDeletePayloadFromInput(nhsNumbers, asArray(inputParticipantRecord));
      const resp = await deleteParticipant(request, payload);
      await composeValidators(expectStatus(200))(resp);
    });

    await test.step('Then DB/API validations should pass', async () => {
      await validateSqlDatabaseFromAPI(request, validations);
    });
  });

  // 7616 — ADD: no NBO exception for blocked participant
  test('@DTOSS-7616-01 ADD: no NBO exception for blocked participant', async ({ request }, testInfo) => {
    const [validations, inputParticipantRecord, nhsNumbers, testFilesPath] =
      await getApiTestData(testInfo.title, 'ADD');

    await cleanupDatabaseFromAPI(request, nhsNumbers);

    await test.step('When a blocked participant ADD is seeded & processed', async () => {
      await seedBlockedThenProcess(request, nhsNumbers, asArray(inputParticipantRecord), testFilesPath!, 'ADD', true);
    });

    await test.step('Then DB/API validations should pass', async () => {
      await validateSqlDatabaseFromAPI(request, validations);
    });
  });

  // 7660 — AMENDED: no NBO exception for blocked participant
  test('@DTOSS-7660-01 AMENDED: no NBO exception for blocked participant', async ({ request }, testInfo) => {
    const [validations, inputParticipantRecord, nhsNumbers, testFilesPath] =
      await getApiTestData(testInfo.title, 'AMENDED');

    await cleanupDatabaseFromAPI(request, nhsNumbers);

    await test.step('When a blocked participant AMENDED is seeded & processed', async () => {
      await seedBlockedThenProcess(request, nhsNumbers, asArray(inputParticipantRecord), testFilesPath!, 'AMENDED', true);
    });

    await test.step('Then DB/API validations should pass', async () => {
      await validateSqlDatabaseFromAPI(request, validations);
    });
  });

  // 7661 — DELETE: no NBO exception for blocked participant
  test('@DTOSS-7661-01 DELETE: no NBO exception for blocked participant', async ({ request }, testInfo) => {
    const [validations, inputParticipantRecord, nhsNumbers, testFilesPath] =
      await getApiTestData(testInfo.title, 'DELETE');

    await cleanupDatabaseFromAPI(request, nhsNumbers);

    await test.step('Given a blocked participant record exists', async () => {
      await seedBlockedThenProcess(request, nhsNumbers, asArray(inputParticipantRecord), testFilesPath!, 'DELETE', true);
    });

    await test.step('When DELETE is requested via API', async () => {
      const payload = buildDeletePayloadFromInput(nhsNumbers, asArray(inputParticipantRecord));
      const resp = await deleteParticipant(request, payload);
      await composeValidators(expectStatus(200))(resp);
    });

    await test.step('Then DB/API validations should pass', async () => {
      await validateSqlDatabaseFromAPI(request, validations);
    });
  });

  // 7663 — ADD: blocked ineligible participant becomes eligible (no NBO)
  test('@DTOSS-7663-01 ADD: blocked ineligible becomes eligible (no NBO)', async ({ request }, testInfo) => {
    const [validations, inputParticipantRecord, nhsNumbers, testFilesPath] =
      await getApiTestData(testInfo.title, 'ADD');

    await cleanupDatabaseFromAPI(request, nhsNumbers);

    await test.step('Given a blocked & ineligible participant is seeded', async () => {
      const seed = withNhs([{ ...asArray(inputParticipantRecord)[0], BlockedFlag: 1, EligibilityFlag: '0' }], nhsNumbers);
      const seedParquet = await createParquetFromJson(nhsNumbers, seed, testFilesPath!);
      await processFileViaStorage(seedParquet);
    });

    await test.step('When the original ADD is processed', async () => {
      const runParquet = await createParquetFromJson(nhsNumbers, withNhs(asArray(inputParticipantRecord), nhsNumbers), testFilesPath!);
      await processFileViaStorage(runParquet);
    });

    await test.step('Then DB/API validations should pass', async () => {
      await validateSqlDatabaseFromAPI(request, validations);
    });
  });

  // 7664 — ADD: audit logs updated for blocked participant
  test('@DTOSS-7664-01 ADD: audit logs updated for blocked participant', async ({ request }, testInfo) => {
    const [validations, inputParticipantRecord, nhsNumbers, testFilesPath] =
      await getApiTestData(testInfo.title, 'ADD');

    await cleanupDatabaseFromAPI(request, nhsNumbers);

    await test.step('When a blocked participant ADD is seeded & processed', async () => {
      await seedBlockedThenProcess(request, nhsNumbers, asArray(inputParticipantRecord), testFilesPath!, 'ADD', true);
    });

    await test.step('Then DB/API validations should pass', async () => {
      await validateSqlDatabaseFromAPI(request, validations);
    });
  });

  // 7665 — AMENDED: audit logs updated for blocked participant
  test('@DTOSS-7665-01 AMENDED: audit logs updated for blocked participant', async ({ request }, testInfo) => {
    const [validations, inputParticipantRecord, nhsNumbers, testFilesPath] =
      await getApiTestData(testInfo.title, 'AMENDED');

    await cleanupDatabaseFromAPI(request, nhsNumbers);

    await test.step('When a blocked participant AMENDED is seeded & processed', async () => {
      await seedBlockedThenProcess(request, nhsNumbers, asArray(inputParticipantRecord), testFilesPath!, 'AMENDED', true);
    });

    await test.step('Then DB/API validations should pass', async () => {
      await validateSqlDatabaseFromAPI(request, validations);
    });
  });

  // 7666 — DELETE: audit logs updated for blocked participant
  test('@DTOSS-7666-01 DELETE: audit logs updated for blocked participant', async ({ request }, testInfo) => {
    const [validations, inputParticipantRecord, nhsNumbers, testFilesPath] =
      await getApiTestData(testInfo.title, 'DELETE');

    await cleanupDatabaseFromAPI(request, nhsNumbers);

    await test.step('Given a blocked participant record exists', async () => {
      await seedBlockedThenProcess(request, nhsNumbers, asArray(inputParticipantRecord), testFilesPath!, 'DELETE', true);
    });

    await test.step('When DELETE is requested via API', async () => {
      const payload = buildDeletePayloadFromInput(nhsNumbers, asArray(inputParticipantRecord));
      const resp = await deleteParticipant(request, payload);
      await composeValidators(expectStatus(200))(resp);
    });

    await test.step('Then DB/API validations should pass', async () => {
      await validateSqlDatabaseFromAPI(request, validations);
    });
  });
});
