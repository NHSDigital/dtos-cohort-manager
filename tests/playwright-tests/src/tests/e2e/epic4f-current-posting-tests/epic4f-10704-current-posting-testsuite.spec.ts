import { expect, test } from '../../fixtures/test-fixtures';
import { config } from '../../../config/env';
import { sendHttpGet, sendHttpPOSTCall } from '../../../api/core/sendHTTPRequest';
import { extractSubscriptionID, retry } from '../../../api/distributionService/bsSelectService';
import { cleanupWireMock, enableMeshOutboxFailureInWireMock, getTestData, resetWireMockMappings, validateMeshRequestWithMockServer, validateSqlDatabaseFromAPI } from '../../steps/steps';
const DEFAULT_NHS_NUMBER = '9997160908';

function buildUrl(base: string, route: string, params: Record<string, string | number> = {}) {
  // Ensure a valid absolute base and preserve any existing query (e.g., function key)
  const u = new URL(route, base);
  Object.entries(params).forEach(([k, v]) => u.searchParams.set(k, String(v)));
  return u.toString();
}

async function checkSubscriptionStatus(nhsNumber: string) {
  // Check endpoint is served by the Manage NEMS Subscription service
  const url = buildUrl(config.SubToNems, config.CheckNemsSubPath, { nhsNumber });
  return await sendHttpGet(url);
}

async function subscribe(nhsNumber: string, body: string = '') {
  // Subscribe endpoint lives on Manage-CAAS (preferred), fallback to Manage-NEMS if not configured
  const base = config.ManageCaasSubscribe || config.SubToNems;
  const url = buildUrl(base, config.SubToNemsPath, { nhsNumber });
  return await sendHttpPOSTCall(url, body);
}

async function unsubscribe(nhsNumber: string) {
  // Unsubscribe support is not implemented; endpoint should return success with "not supported" message per AC
  const base = config.ManageCaasSubscribe || config.SubToNems;
  const url = buildUrl(base, 'api/Unsubscribe', { nhsNumber });
  return await sendHttpPOSTCall(url, '');
}

test.describe.serial('@regression @e2e @epic4f- Current Posting Subscribe/Unsubscribe tests', () => {

  // Auto-enable Mesh WireMock assertions when WIREMOCK_URL is configured
  test.beforeAll(() => {
    const url = config.wireMockUrl?.trim() ?? '';
    // Only enable WireMock assertions when a valid http/https URL is configured
    if (/^https?:\/\//i.test(url)) {
      process.env.USE_MESH_WIREMOCK = '1';
    }
  });

  test('@DTOSS-10704-01 DTOSS-10939 - Successful subscription when participant not already subscribed', async ({ request }, testInfo) => {
    const [_, nhsNumbers] = await getTestData(testInfo.title);
    const freshNhs = (nhsNumbers[0] as any) ?? DEFAULT_NHS_NUMBER;
    if (process.env.USE_MESH_WIREMOCK === '1') {
      await cleanupWireMock(request);
    }
    // Given not subscribed
    const pre = await retry(() => checkSubscriptionStatus(freshNhs), r => [404, 200].includes(r.status), { retries: 3, delayMs: 1500, throwLastError: false });
    expect([404, 200]).toContain(pre.status); // allow 200 in case of prior runs; prefer 404

    // When POST Subscribe
    const resp = await subscribe(freshNhs);
    expect(resp.status).toBe(200);

    if (process.env.USE_MESH_WIREMOCK === '1') {
      await validateMeshRequestWithMockServer(request, { minCount: 1 });
    }

    // Then check status returns 200 and has a subscription id
    const check = await retry(() => checkSubscriptionStatus(freshNhs), r => r.status === 200, { retries: 3, delayMs: 2000, throwLastError: false });
    expect(check.status).toBe(200);
    const checkText = await check.text();
    const subId = extractSubscriptionID({ status: check.status, data: undefined as any, text: checkText } as any);
    if (!subId) {
      await testInfo.attach('subscribe-check-body.txt', { body: checkText, contentType: 'text/plain' });
    }
    expect(subId).not.toBeNull();
  });

  test('@DTOSS-10704-02 DTOSS-10940 - Already subscribed returns success with existing id (idempotent)', async ({ request }, testInfo) => {
    const [_, nhsNumbers] = await getTestData(testInfo.title);
    const subscribedNhs = (nhsNumbers[0] as any) ?? DEFAULT_NHS_NUMBER;
    if (process.env.USE_MESH_WIREMOCK === '1') {
      await cleanupWireMock(request);
    }
    // Ensure subscribed once
    const first = await subscribe(subscribedNhs);
    expect(first.status).toBe(200);

    // Capture current subscription id
    const beforeCheck = await retry(() => checkSubscriptionStatus(subscribedNhs), r => r.status === 200, { retries: 3, delayMs: 1500, throwLastError: false });
    expect(beforeCheck.status).toBe(200);
    const beforeText = await beforeCheck.text();
    const beforeId = extractSubscriptionID({ status: beforeCheck.status, data: undefined as any, text: beforeText } as any);

    // When subscribing again
    const again = await subscribe(subscribedNhs);
    expect(again.status).toBe(200);

    if (process.env.USE_MESH_WIREMOCK === '1') {
      await validateMeshRequestWithMockServer(request, { minCount: 2 });
    }

    // Then status remains subscribed with same id
    const afterCheck = await retry(() => checkSubscriptionStatus(subscribedNhs), r => r.status === 200, { retries: 3, delayMs: 2000, throwLastError: false });
    expect(afterCheck.status).toBe(200);
    const afterText = await afterCheck.text();
    const afterId = extractSubscriptionID({ status: afterCheck.status, data: undefined as any, text: afterText } as any);
    if (afterId !== beforeId) {
      // Extra diagnostics to help investigate idempotency differences in non-prod
      console.info(`Idempotency check mismatch:\nBeforeID=${beforeId}\nAfterID=${afterId}\nBeforeBody=${beforeText}\nAfterBody=${afterText}`);
      await testInfo.attach('idempotent-before-body.txt', { body: beforeText, contentType: 'text/plain' });
      await testInfo.attach('idempotent-after-body.txt', { body: afterText, contentType: 'text/plain' });
    }
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
    const nhs = (nhsNumbers[0] as any) ?? DEFAULT_NHS_NUMBER;

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
    const nhs = (nhsNumbers[0] as any) ?? DEFAULT_NHS_NUMBER;
    const s1 = await subscribe(nhs);
    let subscriptionConfirmed = s1.status === 200;
    if (!subscriptionConfirmed) {
      const s1Text = await s1.text();
      const pre = await checkSubscriptionStatus(nhs);
      subscriptionConfirmed = pre.status === 200;
      if (!subscriptionConfirmed) {
        // Attach for diagnostics but continue: Unsubscribe should still return not-supported regardless.
        await testInfo.attach('unsubscribe-presubscribe-failed.txt', { body: `status=${s1.status}\nbody=${s1Text}`, contentType: 'text/plain' });
      }
    }

    // Call unsubscribe endpoint
    const u = await unsubscribe(nhs);
    expect([200, 501]).toContain(u.status); // 200 with message, or 501 Not Implemented
    const text = (await u.text()).toLowerCase();
    // Accept dev env stub message as well as spec language
    expect(text).toMatch(/not\s+supported|unsub|stub.*removed/);

    // Ensure subscription remains only if we confirmed it was subscribed
    if (subscriptionConfirmed) {
      const check = await checkSubscriptionStatus(nhs);
      expect(check.status).toBe(200);
    } else {
      testInfo.annotations.push({ type: 'Note', description: 'Skip final subscription status check; precondition not met' });
    }
  });
});
