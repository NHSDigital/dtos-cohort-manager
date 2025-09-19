import { expect, test } from '../../fixtures/test-fixtures';
import { config } from '../../../config/env';
import { sendHttpGet, sendHttpPOSTCall } from '../../../api/core/sendHTTPRequest';
import { extractSubscriptionID, retry } from '../../../api/distributionService/bsSelectService';
import { cleanupWireMock, cleanupNemsSubscriptions, enableMeshOutboxFailureInWireMock, enableMeshOutboxSuccessInWireMock, getTestData, removeMeshOutboxMappings, validateMeshRequestWithMockServer, validateSqlDatabaseFromAPI, resetWireMockMappings, cleanupDatabaseFromAPI } from '../../steps/steps';
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
  const wireMockEnabled = () => /^https?:\/\//i.test((config.wireMockUrl ?? '').trim());

  test('@DTOSS-10704-01 DTOSS-10939 - Successful subscription when participant not already subscribed', async ({ request }) => {
    const freshNhs = generateValidNhsNumber();
    await cleanupNemsSubscriptions(request, [freshNhs]);
    if (wireMockEnabled()) {
      await cleanupWireMock(request);
      await removeMeshOutboxMappings(request);
      await enableMeshOutboxSuccessInWireMock(request);
    }
    const pre = await retry(() => checkSubscriptionStatus(freshNhs), r => [404, 200].includes(r.status), { retries: 3, delayMs: 1500, throwLastError: false });
    expect([404, 200]).toContain(pre.status);

    const resp = await subscribe(freshNhs);
    expect(resp.status).toBe(200);

    if (wireMockEnabled()) {
      await validateMeshRequestWithMockServer(request, { minCount: 1, attempts: 8, delayMs: 1500 });
    }

    const check = await retry(() => checkSubscriptionStatus(freshNhs), r => r.status === 200, { retries: 3, delayMs: 2000, throwLastError: false });
    expect(check.status).toBe(200);
    const checkText = await check.text();
    const subId = extractSubscriptionID({ status: check.status, data: undefined as any, text: checkText } as any);
    expect(subId).not.toBeNull();
  });

  test('@DTOSS-10704-02 DTOSS-10940 - Already subscribed returns success with existing id (idempotent)', async ({ request }) => {
    const subscribedNhs = generateValidNhsNumber();
    await cleanupNemsSubscriptions(request, [subscribedNhs]);
    if (process.env.USE_MESH_WIREMOCK === '1') {
      await cleanupWireMock(request);
      await removeMeshOutboxMappings(request);
      await enableMeshOutboxSuccessInWireMock(request);
    }
    const first = await subscribe(subscribedNhs);
    expect(first.status).toBe(200);

    const beforeCheck = await retry(() => checkSubscriptionStatus(subscribedNhs), r => r.status === 200, { retries: 3, delayMs: 1500, throwLastError: false });
    expect(beforeCheck.status).toBe(200);
    const beforeText = await beforeCheck.text();
    const beforeId = extractSubscriptionID({ status: beforeCheck.status, data: undefined as any, text: beforeText } as any);

    if (wireMockEnabled()) {
      await cleanupWireMock(request);
    }

    const again = await subscribe(subscribedNhs);
    expect(again.status).toBe(200);

    if (wireMockEnabled()) {
      await validateMeshRequestWithMockServer(request, { minCount: 0 });
    }

    const afterCheck = await retry(() => checkSubscriptionStatus(subscribedNhs), r => r.status === 200, { retries: 3, delayMs: 2000, throwLastError: false });
    expect(afterCheck.status).toBe(200);
    const afterText = await afterCheck.text();
    const afterId = extractSubscriptionID({ status: afterCheck.status, data: undefined as any, text: afterText } as any);
    expect(afterId).toBe(beforeId);
  });

  test('@DTOSS-10704-03 DTOSS-10941 - Invalid request missing/incorrect NHS number returns validation error', async ({ request }) => {
    if (wireMockEnabled()) {
      await cleanupWireMock(request);
    }
    const noParam = await sendHttpPOSTCall(`${config.SubToNems}${config.SubToNemsPath}`, '');
    expect([400, 404]).toContain(noParam.status);
    const noParamText = (await noParam.text()).toLowerCase();
    expect(noParamText).toMatch(/invalid|missing|nhs/);

    if (process.env.USE_MESH_WIREMOCK === '1') {
      await validateMeshRequestWithMockServer(request, { minCount: 0 });
    }

    const invalidParam = await sendHttpPOSTCall(`${config.SubToNems}${config.SubToNemsPath}?nhsNumber=abc`, '');
    expect([400, 422]).toContain(invalidParam.status);
    const invalidText = (await invalidParam.text()).toLowerCase();
    expect(invalidText).toMatch(/invalid|nhs/);

    if (wireMockEnabled()) {
      await validateMeshRequestWithMockServer(request, { minCount: 0 });
    }
  });

  test('@DTOSS-10704-04 DTOSS-10942 - Failure to send to Mesh logs exception and no subscription (conditional)', async ({ request }) => {
    const nhs = generateValidNhsNumber();
    process.env.EPIC4F_04_NHS = nhs;

    await cleanupNemsSubscriptions(request, [nhs]);
    // Reset exception table for this NHS and set deterministic WireMock mapping when configured
    await cleanupDatabaseFromAPI(request, [nhs], ['exceptionManagement']);
    if (wireMockEnabled()) {
      await cleanupWireMock(request);
      await removeMeshOutboxMappings(request);
      await enableMeshOutboxFailureInWireMock(request, 500);
    }

    const resp = await subscribe(nhs);

    const check = await checkSubscriptionStatus(nhs);
    expect([404]).toContain(check.status);

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

    if (wireMockEnabled()) {
      await resetWireMockMappings(request);
      await enableMeshOutboxSuccessInWireMock(request);
    }
  });

  test('@DTOSS-10704-05 DTOSS-10943 - Unsubscribe returns success with not supported', async ({}, testInfo) => {
    const [_, nhsNumbers] = await getTestData(testInfo.title);
    const nhs = (nhsNumbers[0] as any) ?? DEFAULT_NHS_NUMBER;
    const s1 = await subscribe(nhs);
    let subscriptionConfirmed = s1.status === 200;
    if (!subscriptionConfirmed) {
      const pre = await checkSubscriptionStatus(nhs);
      subscriptionConfirmed = pre.status === 200;
    }

    const u = await unsubscribe(nhs);
    expect(u.status).toBe(200);
    const text = (await u.text()).toLowerCase();
    expect(text).toMatch(/not\s+supported|unsub|stub.*removed/);

    if (subscriptionConfirmed) {
      const check = await checkSubscriptionStatus(nhs);
      expect(check.status).toBe(200);
    }
  });
});
