import { test, expect } from '@playwright/test';
import { getValidationExceptions } from '../../../../api/dataService/exceptionService';
import { getApiQueryParams } from '../../../steps/steps';

test.describe('@DTOSS-9609-01 - Verify GetValidationExceptions API responses', () => {
  let apiConfig: any;
  let response: any;

  test.beforeAll(async ({ request }) => {
    apiConfig = await getApiQueryParams('@DTOSS-9609-01 - Verify GetValidationExceptions API responses', 'validation_exceptions_sample_data');
    response = await getValidationExceptions(request, apiConfig);
  });

  test('Should return 204 if no data is found or 200 with records', async () => {
    expect([200, 204]).toContain(response.status);

    if (response.status === 204) {
      console.info('Verified 204 response when no data is found');
    } else {
      expect(response.data).toBeDefined();
      expect(response.data.Items).toBeDefined();
      expect(Array.isArray(response.data.Items)).toBe(true);
      expect(response.data.Items.length).toBeGreaterThan(0);
      console.info(`Verified 200 response with ${response.data.Items.length} records`);
    }
  });

  test('Should verify all returned records have ServiceNowId  and ServiceNowCreatedDate', async () => {
    if (response.status === 204) {
      console.info('No data found - skipping validation test');
      return;
    }

    const items = response.data.Items;

    items.forEach((item: any, index: number) => {
      expect(item.ServiceNowId).toBeTruthy();
      expect(item.ServiceNowCreatedDate ).toBeTruthy();

      console.info(`Record ${index + 1}: ServiceNow ID: ${item.ServiceNowId }, Created Date: ${item.ServiceNowCreatedDate}`);
    });

    console.info(`Verified all ${items.length} records have required ServiceNowId  and ServiceNowCreatedDate fields`);
  });

  test('Should verify records are sorted in descending order by ServiceNowCreatedDate', async () => {
    if (response.status === 204) {
      console.info('No data found - skipping sorting test');
      return;
    }

    const items = response.data.Items;

    if (items.length <= 1) {
      console.info(`Only ${items.length} record(s) found - sorting verification not applicable`);
      return;
    }

    for (let i = 1; i < items.length; i++) {
      const prevDate = new Date(items[i - 1].ServiceNowCreatedDate);
      const currDate = new Date(items[i].ServiceNowCreatedDate);

      expect(prevDate.getTime()).toBeGreaterThanOrEqual(currDate.getTime());
    }

    console.info(`Verified ${items.length} records are sorted in descending order by ServiceNowCreatedDate`);
  });
});
