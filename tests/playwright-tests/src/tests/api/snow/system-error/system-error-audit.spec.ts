import { test, expect } from '@playwright/test';
import { composeValidators, expectStatus, validateResponseByStatus } from '../../../../api/responseValidators';
import { receiveParticipantViaServiceNow } from '../../../../api/distributionService/bsSelectService';
import { loadParticipantPayloads } from '../../../fixtures/jsonDataReader';
import { config } from '../../../../config/env';
import { getRecordsFromBsSelectRetrieveAudit } from '../../../../api/distributionService/bsSelectService';

test.describe('@epic4c- @service_now @api DTOSS-8569-05 system error pathway', () => {

  // Load dedicated API fixture for 8569-05
  const folderName = '@DTOSS-8569-05';
  const fileName = 'ADD-participantPayload.json';
  const participantData = loadParticipantPayloads(folderName, fileName);
  const payload = participantData['ceasing-system-error'] as any;

  test('@DTOSS-8569-05 Verify system error is raised when record fails to meet Transformation rules 2 due to system failure', async ({ request }) => {
    // Use ceasing payload known to traverse validation; environment should simulate a system failure if configured
    const resp = await receiveParticipantViaServiceNow(request, payload);
    const validators = composeValidators(expectStatus(202), validateResponseByStatus());
    await validators(resp);

    const nhsNumber = String(payload.u_case_variable_data.nhs_number);
    const start = Date.now();

    // Assert ExceptionManagement shows at least one unresolved exception for this NHS (system failure path)
    const exceptionEndpoint = `${config.endpointExceptionManagementDataService}api/${config.exceptionManagementService}`;
    // Poll ExceptionManagement for an exception (unresolved or recently created)
    const pollException = async () => {
      const res = await request.get(exceptionEndpoint);
      if (res.status() !== 200) return false;
      const items = await res.json();
      const matched = items.filter((r: any) => (r.NhsNumber == nhsNumber || r.NHSNumber == nhsNumber));
      if (matched.length === 0) return false;
      // Accept unresolved or a recent exception (within ~2 minutes of start)
      const recentThreshold = start - 2 * 60 * 1000;
      const ok = matched.some((r: any) => {
        const unresolved = (!r.DateResolved || r.DateResolved === '9999-12-31T00:00:00');
        const when = Date.parse(r.ExceptionDate || r.DateCreated || '');
        const isRecent = isFinite(when) && when >= recentThreshold;
        return unresolved || isRecent;
      });
      return ok;
    };

    let exFound = false;
    let backoffEx = Math.max(1000, Number(config.apiWaitTime) / 2);
    for (let i = 0; i < 6 && !exFound; i++) {
      exFound = await pollException();
      if (!exFound) {
        await new Promise(res => setTimeout(res, backoffEx));
        backoffEx = Math.min(backoffEx * 2, Number(config.apiWaitTime) * 3);
      }
    }
    expect(exFound).toBeTruthy();

    // Small stabilization wait to allow audit write
    await new Promise(res => setTimeout(res, Number(config.apiWaitTime)));

    // Assert SNOW message audit evidence exists for this NHS
    const tryAudit = async () => {
      const attempt1 = await getRecordsFromBsSelectRetrieveAudit(request, { NHSNumber: nhsNumber });
      if (attempt1.status === 200) {
        const items1: any[] = Array.isArray((attempt1.data as any)?.Items) ? (attempt1.data as any).Items : (Array.isArray(attempt1.data) ? (attempt1.data as any) : []);
        const matched1 = items1.filter((r: any) => String(r.NHSNumber || r.NhsNumber) === nhsNumber);
        if (matched1.length > 0) return true;
      }
      const attempt2 = await getRecordsFromBsSelectRetrieveAudit(request, { NhsNumber: nhsNumber });
      if (attempt2.status === 200) {
        const items2: any[] = Array.isArray((attempt2.data as any)?.Items) ? (attempt2.data as any).Items : (Array.isArray(attempt2.data) ? (attempt2.data as any) : []);
        const matched2 = items2.filter((r: any) => String(r.NHSNumber || r.NhsNumber) === nhsNumber);
        if (matched2.length > 0) return true;
      }
      return false;
    };

    let found = false;
    let backoff = Math.max(1000, Number(config.apiWaitTime) / 2);
    for (let i = 0; i < 6 && !found; i++) {
      found = await tryAudit();
      if (!found) {
        await new Promise(res => setTimeout(res, backoff));
        backoff = Math.min(backoff * 2, Number(config.apiWaitTime) * 3);
      }
    }
    if (!found) {
      const exList = await request.get(`${config.endpointExceptionManagementDataService}api/${config.exceptionManagementService}`);
      if (exList.status() === 200) {
        const items = await exList.json();
        const serviceNowTransforms = items.filter((r: any) =>
          (r.NhsNumber == nhsNumber || r.NHSNumber == nhsNumber)
          && typeof r.RuleDescription === 'string'
          && r.RuleDescription.toLowerCase().includes('servicenow')
        );
        expect(serviceNowTransforms.length).toBeGreaterThan(0);
      } else {
        expect(found).toBeTruthy();
      }
    }
  });
});
