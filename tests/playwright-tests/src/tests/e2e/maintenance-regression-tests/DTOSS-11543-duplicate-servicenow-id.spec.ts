import { test, expect } from '@playwright/test';
import { composeValidators, expectStatus } from '../../../api/responseValidators';
import { receiveParticipantViaServiceNow } from '../../../api/distributionService/bsSelectService';
import { loadParticipantPayloads } from '../../fixtures/jsonDataReader';
import { ParticipantRecord } from '../../../interface/InputData';
import { fetchApiResponse } from '../../../api/apiHelper';
import { config } from '../../../config/env';

// Maintenance regression test for DTOSS-11543 duplicate ServiceNow ID handling
// Tags include maintenance-regression to allow selective execution

test.describe.serial('@maintenance-regression @DTOSS-11543 @epic4c @api @duplicate-servicenow-id Test duplicate ServiceNow case ID handling', () => {
  let participantData: Record<string, ParticipantRecord>;
  const DUPLICATE_CASE_ID = 'CS9999999';
  const SERVICE_NOW_DATA_SERVICE = config.serviceNowCasesDataService;

  test.beforeAll(async () => {
    const folderName = '@duplicate-servicenow-id';
    const fileName = 'duplicate-servicenow-id-payload.json';
    participantData = await loadParticipantPayloads(folderName, fileName);
  });

  test('@DTOSS-11543-1 First request with ServiceNow case ID should succeed', async ({ request }, testInfo) => {
    testInfo.annotations.push({
      type: 'Requirement - https://nhsd-jira.digital.nhs.uk/browse/DTOSS-11543',
      description: 'Verifies that duplicate ServiceNow requests (10 seconds apart) do not cause exceptions',
    });

    const payload = participantData['duplicateServiceNowId-first'];

    await test.step('Given a valid participant is received from ServiceNow, then response code is 202', async () => {
      const response = await receiveParticipantViaServiceNow(request, payload);
      const validators = composeValidators(
        expectStatus(202)
      );
      await validators(response);
    });
  });

  test('@DTOSS-11543-2 Second request with SAME ServiceNow case ID should also succeed (no primary key collision)', async ({ request }, testInfo) => {
    testInfo.annotations.push({
      type: 'Requirement - https://nhsd-jira.digital.nhs.uk/browse/DTOSS-11543',
      description: 'Verifies that duplicate ServiceNow requests with same case ID do not cause primary key violations',
    });

    const payload = participantData['duplicateServiceNowId-second'];

    await test.step('Given a duplicate ServiceNow case ID is received (simulating retry 10 seconds later), then response code is 202', async () => {
      // Wait 10 seconds to simulate the actual scenario described in the bug
      console.info('⏰ Waiting 10 seconds to simulate duplicate ServiceNow request...');
      await new Promise(res => setTimeout(res, 10000));

      const response = await receiveParticipantViaServiceNow(request, payload);
      const validators = composeValidators(
        expectStatus(202)
      );
      await validators(response);
    });
  });

  test('@DTOSS-11543-3 Verify both records exist in database with different GUIDs', async ({ request }, testInfo) => {
    testInfo.annotations.push({
      type: 'Requirement - https://nhsd-jira.digital.nhs.uk/browse/DTOSS-11543',
      description: 'Verifies that both records with the same ServiceNow case ID exist in database with unique GUIDs',
    });

    await test.step('Query ServiceNowCasesDataService to get all records with the duplicate case ID', async () => {
      // Wait a bit for the data to be persisted
      await new Promise(res => setTimeout(res, 2000));

      const response = await fetchApiResponse(`api/${SERVICE_NOW_DATA_SERVICE}`, request);
      expect(response.ok()).toBeTruthy();

      const allRecords = await response.json();
      expect(Array.isArray(allRecords)).toBeTruthy();

      // Filter records by ServicenowId
      const duplicateRecords = allRecords.filter((record: any) => record.ServicenowId === DUPLICATE_CASE_ID);

      console.info(`Found ${duplicateRecords.length} records with ServiceNow case ID ${DUPLICATE_CASE_ID}`);
      console.info('Records:', JSON.stringify(duplicateRecords, null, 2));

      // Verify we have at least 2 records with the same ServicenowId
      expect(duplicateRecords.length).toBeGreaterThanOrEqual(2);

      // Extract the ID (GUID) values
      const ids = duplicateRecords.map((record: any) => record.Id);

      // Verify all IDs are unique (no duplicates in the array)
      const uniqueIds = new Set(ids);
      expect(uniqueIds.size).toBe(ids.length);

      // Verify all IDs are valid GUIDs (format check)
      const guidRegex = /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;
      for (const id of ids) {
        expect(id).toMatch(guidRegex);
      }

      console.info(`✅ Verified ${duplicateRecords.length} records with same ServiceNowId but unique GUIDs`);
      console.info('Unique IDs:', ids);
    });
  });
});
