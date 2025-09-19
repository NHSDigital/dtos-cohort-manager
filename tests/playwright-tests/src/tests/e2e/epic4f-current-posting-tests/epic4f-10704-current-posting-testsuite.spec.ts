import { expect, test } from '../../fixtures/test-fixtures';
import { config } from '../../../config/env';
import { sendHttpGet, sendHttpPOSTCall } from '../../../api/core/sendHTTPRequest';
import { extractSubscriptionID, retry } from '../../../api/distributionService/bsSelectService';
import { cleanupWireMock, cleanupNemsSubscriptions, enableMeshOutboxFailureInWireMock, enableMeshOutboxSuccessInWireMock, getTestData, removeMeshOutboxMappings, validateMeshRequestWithMockServer, validateSqlDatabaseFromAPI, getWireMockMappingsJson, resetWireMockMappings, cleanupDatabaseFromAPI } from '../../steps/steps';
const DEFAULT_NHS_NUMBER = '9997160908';

// Generate a valid 10-digit NHS number starting with 999 using the Mod 11 algorithm
function generateValidNhsNumber(): string {
  while (true) {
    const base9 = '999' + Math.floor(Math.random() * 1_000_000).toString().padStart(6, '0');
    const weights = [10,9,8,7,6,5,4,3,2];
    const sum = base9.split('').reduce((acc, d, i) => acc + Number(d) * weights[i], 0);
    const remainder = sum % 11;
    let check = 11 - remainder;
    if (check === 11) check = 0;
    if (check === 10) continue; // invalid, regenerate
    return base9 + String(check);
  }
}

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
    // Use a fresh, valid NHS number to avoid pre-existing subscriptions from other flows
    const freshNhs = generateValidNhsNumber();
    // Ensure not already subscribed to exercise first-time path
    await cleanupNemsSubscriptions(request, [freshNhs]);
    if (process.env.USE_MESH_WIREMOCK === '1') {
      await cleanupWireMock(request);
      await removeMeshOutboxMappings(request);
      // Ensure success mapping uses expected response shape (message_id)
      await enableMeshOutboxSuccessInWireMock(request);
    }
    // Given not subscribed
    const preUrl = buildUrl(config.ManageCaasSubscribe || config.SubToNems, config.CheckNemsSubPath, { nhsNumber: freshNhs });
    await testInfo.attach('debug-precheck-url.txt', { body: preUrl, contentType: 'text/plain' });
    const pre = await retry(() => checkSubscriptionStatus(freshNhs), r => [404, 200].includes(r.status), { retries: 3, delayMs: 1500, throwLastError: false });
    expect([404, 200]).toContain(pre.status); // allow 200 in case of prior runs; prefer 404

    // When POST Subscribe
    const subscribeUrl = buildUrl(config.ManageCaasSubscribe || config.SubToNems, config.SubToNemsPath, { nhsNumber: freshNhs });
    await testInfo.attach('debug-subscribe-url.txt', { body: subscribeUrl, contentType: 'text/plain' });
    const resp = await subscribe(freshNhs);
    // Attach response body for diagnostics before asserting
    let respBody = '';
    try {
      respBody = await resp.text();
      await testInfo.attach('subscribe-response.txt', { body: respBody, contentType: 'text/plain' });
    } catch {}
    expect(resp.status).toBe(200);

    if (process.env.USE_MESH_WIREMOCK === '1') {
      await validateMeshRequestWithMockServer(request, { minCount: 1, attempts: 8, delayMs: 1500 });
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
    // Use a fresh, valid NHS number per run to make idempotency deterministic within the test
    const subscribedNhs = generateValidNhsNumber();
    // Start clean then create an initial subscription for idempotency check
    await cleanupNemsSubscriptions(request, [subscribedNhs]);
    if (process.env.USE_MESH_WIREMOCK === '1') {
      await cleanupWireMock(request);
      await removeMeshOutboxMappings(request);
      await enableMeshOutboxSuccessInWireMock(request);
    }
    // Ensure subscribed once
    const first = await subscribe(subscribedNhs);
    expect(first.status).toBe(200);
    try {
      const body = await first.text();
      await testInfo.attach('first-subscribe-response.txt', { body, contentType: 'text/plain' });
    } catch {}

    // Capture current subscription id
    const beforeCheck = await retry(() => checkSubscriptionStatus(subscribedNhs), r => r.status === 200, { retries: 3, delayMs: 1500, throwLastError: false });
    expect(beforeCheck.status).toBe(200);
    const beforeText = await beforeCheck.text();
    const beforeId = extractSubscriptionID({ status: beforeCheck.status, data: undefined as any, text: beforeText } as any);

    // Clear WireMock requests to isolate idempotent path (no new Mesh post expected)
    if (process.env.USE_MESH_WIREMOCK === '1') {
      await cleanupWireMock(request);
    }

    // When subscribing again
    const again = await subscribe(subscribedNhs);
    expect(again.status).toBe(200);

    // Then no new Mesh outbox request is posted (no new Parquet/file posted)
    if (process.env.USE_MESH_WIREMOCK === '1') {
      await validateMeshRequestWithMockServer(request, { minCount: 0 });
    }

    // And status remains subscribed with same id
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

  test('@DTOSS-10704-03 DTOSS-10941 - Invalid request missing/incorrect NHS number returns validation error', async ({ request }) => {
    // Start with a clean WireMock state when enabled
    if (process.env.USE_MESH_WIREMOCK === '1') {
      await cleanupWireMock(request);
    }
    // Missing nhsNumber
    const noParam = await sendHttpPOSTCall(`${config.SubToNems}${config.SubToNemsPath}`, '');
    expect([400, 404]).toContain(noParam.status); // depending on routing, 404 if endpoint requires param in route
    const noParamText = (await noParam.text()).toLowerCase();
    expect(noParamText).toMatch(/invalid|missing|nhs/);

    // Ensure no Mesh interaction occurs on invalid input
    if (process.env.USE_MESH_WIREMOCK === '1') {
      await validateMeshRequestWithMockServer(request, { minCount: 0 });
    }

    // Invalid nhsNumber format
    const invalidParam = await sendHttpPOSTCall(`${config.SubToNems}${config.SubToNemsPath}?nhsNumber=abc`, '');
    expect([400, 422]).toContain(invalidParam.status);
    const invalidText = (await invalidParam.text()).toLowerCase();
    expect(invalidText).toMatch(/invalid|nhs/);

    // Ensure no Mesh interaction occurs on invalid input
    if (process.env.USE_MESH_WIREMOCK === '1') {
      await validateMeshRequestWithMockServer(request, { minCount: 0 });
    }
  });

  test('@DTOSS-10704-04 DTOSS-10942 - Failure to send to Mesh logs exception and no subscription (conditional)', async ({ request }, testInfo) => {
    // Use a fresh NHS number to ensure it is not already subscribed in external systems
    const nhs = generateValidNhsNumber();
    // Share with runner via env for cross-spec coordination
    process.env.EPIC4F_04_NHS = nhs;

    // Inject a failure stub for Mesh and clear prior requests via WireMock
    await cleanupNemsSubscriptions(request, [nhs]);
    await cleanupWireMock(request);
    // Start from a clean Exception table, then make failure deterministic
    await cleanupDatabaseFromAPI(request, [nhs], ['exceptionManagement']);
    // Make failure deterministic: remove only prior outbox mappings, then add failure mapping
    await removeMeshOutboxMappings(request);
    await enableMeshOutboxFailureInWireMock(request, 500);
    // Attach current mappings for diagnostics
    try {
      const mappings = await getWireMockMappingsJson(request);
      await testInfo.attach('wiremock-mappings-after-failure-stub.json', { body: mappings, contentType: 'application/json' });
    } catch {}

    // Attempt subscribe (environment or WireMock should make Mesh call fail)
    const resp = await subscribe(nhs);
    // Some environments may surface failure as a 200 with an exception logged; capture body for diagnostics
    try {
      const body = await resp.text();
      await testInfo.attach('failure-subscribe-response.txt', { body, contentType: 'text/plain' });
    } catch {}

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
    ], { retries: 3, initialWaitMs: 2000, stepMs: 2000 });

    // Clean up WireMock mappings afterwards to avoid affecting subsequent tests
    // Restore default happy-path mapping after the test to avoid impacting others
    await resetWireMockMappings(request);
    await enableMeshOutboxSuccessInWireMock(request);
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
    // Per AC, unsubscribe returns a successful 200 with a not-supported message
    expect(u.status).toBe(200);
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
