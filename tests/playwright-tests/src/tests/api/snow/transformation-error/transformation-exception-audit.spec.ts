import { test, expect } from '@playwright/test';
import { composeValidators, expectStatus, validateResponseByStatus } from '../../../../api/responseValidators';
import { receiveParticipantViaServiceNow } from '../../../../api/distributionService/bsSelectService';
import { loadParticipantPayloads } from '../../../fixtures/jsonDataReader';
import { config } from '../../../../config/env';
// Note: Removed RetrieveCohortRequestAudit usage; it's unrelated to ServiceNow

test.describe('@epic4c- @service_now @api DTOSS-8569-04 transformation failure pathway', () => {
  // Load dedicated API fixture for 8569-04
  const folderName = '@DTOSS-8569-04';
  const fileName = 'ADD-participantPayload.json';
  const participantData = loadParticipantPayloads(folderName, fileName);
  const payload = participantData['ceasing-transformation-failure'];

  test('@DTOSS-8569-04 Exception is raised when record fails Transformation rules (evidenced via ExceptionManagement)', async ({ request }) => {
    // Use the known exception-path NHS from 4C scenarios
    const response = await receiveParticipantViaServiceNow(request, payload);
    const validators = composeValidators(expectStatus(202), validateResponseByStatus());
    await validators(response);

    const nhsNumber = String(payload.u_case_variable_data.nhs_number);

    // Assert ExceptionManagement has at least one unresolved exception for this NHS (gate for audit)
    const exceptionEndpoint = `${config.endpointExceptionManagementDataService}api/${config.exceptionManagementService}`;
    let exPollAttempts = 0;
    const pollException = async () => {
      exPollAttempts += 1;
      const res = await request.get(exceptionEndpoint);
      if (res.status() !== 200) return false;
      const items = await res.json();
      const unresolved = items.filter((r: any) => (r.NhsNumber == nhsNumber || r.NHSNumber == nhsNumber) && (!r.DateResolved || r.DateResolved === '9999-12-31T00:00:00'));
      return unresolved.length > 0;
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
    console.log(`[DTOSS-8569-04] Exception poll for NHS ${nhsNumber}: found=${exFound} after ${exPollAttempts} attempts`);
    expect(exFound).toBeTruthy();

    // Verify ServiceNow-related evidence via ExceptionManagement
    const exList = await request.get(`${config.endpointExceptionManagementDataService}api/${config.exceptionManagementService}`);
    expect(exList.status(), 'ExceptionManagement endpoint').toBe(200);
    const allItems = await exList.json();
    const serviceNowRelated = allItems.filter((r: any) =>
      (r.NhsNumber == nhsNumber || r.NHSNumber == nhsNumber)
      && typeof r.RuleDescription === 'string'
      && r.RuleDescription.toLowerCase().includes('servicenow')
    );
    console.log(`[DTOSS-8569-04] ExceptionManagement ServiceNow-related entries for NHS ${nhsNumber}: ${serviceNowRelated.length}`);
    expect(serviceNowRelated.length).toBeGreaterThan(0);
  });
});
