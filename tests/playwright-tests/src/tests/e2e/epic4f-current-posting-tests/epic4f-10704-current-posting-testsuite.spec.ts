import { expect, test } from '../../fixtures/test-fixtures';
import { config } from '../../../config/env';
import { sendHttpGet, sendHttpPOSTCall } from '../../../api/core/sendHTTPRequest';
import { extractSubscriptionID } from '../../../api/distributionService/bsSelectService';
import { cleanupWireMock, enableMeshOutboxFailureInWireMock, getTestData, resetWireMockMappings, validateMeshRequestWithMockServer, validateSqlDatabaseFromAPI } from '../../steps/steps';

function computeNhsCheckDigit(nineDigits: string): number {
  let sum = 0;
  for (let i = 0; i < 9; i++) {
    const weight = 10 - i;
    sum += Number(nineDigits[i]) * weight;
  }
  const remainder = sum % 11;
  const check = 11 - remainder;
  if (check === 11) return 0;
  if (check === 10) return -1; // invalid
  return check;
}

function generateValidNhsNumber(prefix: string = '999'): string {
  // Generate a valid 10-digit NHS number beginning with the prefix
  // Ensures checksum (Mod 11) is valid and avoids the '10' invalid case
  while (true) {
    let base = prefix;
    // Fill to 9 digits
    while (base.length < 9) {
      base += Math.floor(Math.random() * 10).toString();
    }
    const check = computeNhsCheckDigit(base);
    if (check >= 0) {
      return base + String(check);
    }
  }
}

async function checkSubscriptionStatus(nhsNumber: string) {
  const url = `${config.SubToNems}${config.CheckNemsSubPath}?nhsNumber=${nhsNumber}`;
  return await sendHttpGet(url);
}

async function subscribe(nhsNumber: string, body: string = '') {
  const url = `${config.SubToNems}${config.SubToNemsPath}?nhsNumber=${nhsNumber}`;
  return await sendHttpPOSTCall(url, body);
}

async function unsubscribe(nhsNumber: string) {
  // Unsubscribe support is not implemented; endpoint should return success with "not supported" message per AC
  const url = `${config.SubToNems}api/Unsubscribe?nhsNumber=${nhsNumber}`;
  return await sendHttpPOSTCall(url, '');
}

test.describe.serial('@regression @e2e @epic4f- Current Posting Subscribe/Unsubscribe tests', () => {

  // Auto-enable Mesh WireMock assertions when WIREMOCK_URL is configured
  test.beforeAll(() => {
    if (config.wireMockUrl && config.wireMockUrl.length > 0) {
      process.env.USE_MESH_WIREMOCK = '1';
    }
  });

  test('@DTOSS-10704-01 DTOSS-10939 - Successful subscription when participant not already subscribed', async ({ request }, testInfo) => {
    const [_, nhsNumbers] = await getTestData(testInfo.title);
    const freshNhs = nhsNumbers[0] ?? generateValidNhsNumber();
    if (process.env.USE_MESH_WIREMOCK === '1') {
      await cleanupWireMock(request);
    }
    // Given not subscribed
    const pre = await checkSubscriptionStatus(freshNhs);
    expect([404, 200]).toContain(pre.status); // allow 200 in case of prior runs; prefer 404

    // When POST Subscribe
    const resp = await subscribe(freshNhs);
    expect(resp.status).toBe(200);

    if (process.env.USE_MESH_WIREMOCK === '1') {
      await validateMeshRequestWithMockServer(request, { minCount: 1 });
    }

    // Then check status returns 200 and has a subscription id
    const check = await checkSubscriptionStatus(freshNhs);
    expect(check.status).toBe(200);
    const subId = extractSubscriptionID({ status: check.status, data: undefined as any, text: await check.text() } as any);
    expect(subId).not.toBeNull();
  });

  test('@DTOSS-10704-02 DTOSS-10940 - Already subscribed returns success with existing id (idempotent)', async ({ request }, testInfo) => {
    const [_, nhsNumbers] = await getTestData(testInfo.title);
    const subscribedNhs = nhsNumbers[0] ?? generateValidNhsNumber();
    if (process.env.USE_MESH_WIREMOCK === '1') {
      await cleanupWireMock(request);
    }
    // Ensure subscribed once
    const first = await subscribe(subscribedNhs);
    expect(first.status).toBe(200);

    // Capture current subscription id
    const beforeCheck = await checkSubscriptionStatus(subscribedNhs);
    expect(beforeCheck.status).toBe(200);
    const beforeId = extractSubscriptionID({ status: beforeCheck.status, data: undefined as any, text: await beforeCheck.text() } as any);

    // When subscribing again
    const again = await subscribe(subscribedNhs);
    expect(again.status).toBe(200);

    if (process.env.USE_MESH_WIREMOCK === '1') {
      await validateMeshRequestWithMockServer(request, { minCount: 2 });
    }

    // Then status remains subscribed with same id
    const afterCheck = await checkSubscriptionStatus(subscribedNhs);
    expect(afterCheck.status).toBe(200);
    const afterId = extractSubscriptionID({ status: afterCheck.status, data: undefined as any, text: await afterCheck.text() } as any);
    expect(afterId).toBe(beforeId);
  });

  test('@DTOSS-10704-03 DTOSS-10941 - Invalid request missing/incorrect NHS number returns validation error', async () => {
    // Missing nhsNumber
    const noParam = await sendHttpPOSTCall(`${config.SubToNems}${config.SubToNemsPath}`, '');
    expect([400, 404]).toContain(noParam.status); // depending on routing, 404 if endpoint requires param in route
    const noParamText = (await noParam.text()).toLowerCase();
    expect(noParamText).toMatch(/invalid|missing|nhs/);

    // Invalid nhsNumber format
    const invalidParam = await sendHttpPOSTCall(`${config.SubToNems}${config.SubToNemsPath}?nhsNumber=abc`, '');
    expect([400, 422]).toContain(invalidParam.status);
    const invalidText = (await invalidParam.text()).toLowerCase();
    expect(invalidText).toMatch(/invalid|nhs/);
  });

  test('@DTOSS-10704-04 DTOSS-10942 - Failure to send to Mesh logs exception and no subscription (conditional)', async ({ request }, testInfo) => {
    // Only run when Mesh WireMock is enabled
    test.skip(process.env.USE_MESH_WIREMOCK !== '1', 'Skipping Mesh failure test; enable USE_MESH_WIREMOCK=1');
    const [_, nhsNumbers] = await getTestData(testInfo.title);
    const nhs = nhsNumbers[0] ?? generateValidNhsNumber();

    // If using WireMock, inject a failure stub for Mesh and clear prior requests
    const usingWireMock = process.env.USE_MESH_WIREMOCK === '1';
    if (usingWireMock) {
      await cleanupWireMock(request);
      await enableMeshOutboxFailureInWireMock(request, 500);
    }

    // Attempt subscribe (environment or WireMock should make Mesh call fail)
    const resp = await subscribe(nhs);
    expect(resp.status).toBeGreaterThanOrEqual(400);

    // Expect not subscribed
    const check = await checkSubscriptionStatus(nhs);
    expect([404]).toContain(check.status);

    // And an exception should be logged for the NHS number (reuse validation helper)
    await validateSqlDatabaseFromAPI(request, [
      {
        validations: {
          apiEndpoint: 'api/ExceptionManagementDataService',
          NhsNumber: Number(nhs)
        },
        meta: {
          testJiraId: '@DTOSS-10704-04',
          requirementJiraId: 'DTOSS-10942',
          additionalTags: '@epic4f- @e2e Mesh failure exception present'
        }
      }
    ]);

    // Clean up WireMock mappings afterwards to avoid affecting subsequent tests
    if (usingWireMock) {
      await resetWireMockMappings(request);
    }
  });

  test('@DTOSS-10704-05 DTOSS-10943 - Unsubscribe returns success with not supported', async ({}, testInfo) => {
    // Prepare a subscription to ensure known state
    const [_, nhsNumbers] = await getTestData(testInfo.title);
    const nhs = nhsNumbers[0] ?? generateValidNhsNumber();
    const s1 = await subscribe(nhs);
    expect(s1.status).toBe(200);

    // Call unsubscribe endpoint
    const u = await unsubscribe(nhs);
    expect([200, 501]).toContain(u.status); // 200 with message, or 501 Not Implemented
    const text = (await u.text()).toLowerCase();
    expect(text).toMatch(/not\s+supported|unsub/);

    // Ensure subscription remains
    const check = await checkSubscriptionStatus(nhs);
    expect(check.status).toBe(200);
  });
});
