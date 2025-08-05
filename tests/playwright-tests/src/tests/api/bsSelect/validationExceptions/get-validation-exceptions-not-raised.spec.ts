import { test, expect } from '@playwright/test';
import { getValidationExceptions } from '../../../../api/dataService/exceptionService';
import { getApiQueryParams } from '../../../steps/steps';

test.describe('@DTOSS-9609-01 - Verify GetValidationExceptions API responses', () => {
  let apiConfig: any;
  let response: any;

  test.beforeAll(async ({ request }) => {
    apiConfig = await getApiQueryParams('@DTOSS-9609-01 - Verify GetValidationExceptions API responses', 'validation_exceptions_not_raised');
    response = await getValidationExceptions(request, apiConfig);
  });

  test('Should return 204 if no data is found or 200 with records', async () => {
    expect(response).toBeDefined();
    expect(response.status).toBeDefined();
    expect([200, 204]).toContain(response.status);

    if (response.status === 204) {
      console.info('Verified 204 response when no data is found');
      expect(response.data).toBeNull();
    } else {
      expect(response.data).toBeDefined();
      expect(response.data.Items).toBeDefined();
      expect(Array.isArray(response.data.Items)).toBe(true);
      expect(response.data.Items.length).toBeGreaterThan(0);
      console.info(`Verified 200 response with ${response.data.Items.length} records`);
    }
  });

  test('Should verify all returned records do NOT have ServiceNow fields and have DateCreated', async () => {
    if (response.status === 204) {
      console.info('No data found - skipping validation test');
      return;
    }

    const items = response.data.Items;
    expect(response.status).toBe(200);
    expect(items).toBeDefined();
    expect(Array.isArray(items)).toBe(true);
    expect(items.length).toBeGreaterThan(0);

    items.forEach((item: any, index: number) => {
      expect(item).toBeDefined();
      expect(item.ServiceNowId).toBeFalsy();
      expect(item.ServiceNowCreatedDate).toBeFalsy();
      expect(item.DateCreated).toBeTruthy();
      expect(item.DateCreated.length).toBeGreaterThan(0);

      const dateObj = new Date(item.DateCreated);
      expect(dateObj).toBeInstanceOf(Date);
      expect(dateObj.getTime()).not.toBeNaN();

      console.info(`Record ${index + 1}: Date Created: ${item.DateCreated}`);
    });

    console.info(`Verified all ${items.length} records have required DateCreated field and no ServiceNow fields`);
  });

  test('Should verify records are sorted in descending order by DateCreated', async () => {
    if (response.status === 204) {
      console.info('No data found - skipping sorting test');
      return;
    }

    const items = response.data.Items;
    expect(response.status).toBe(200);
    expect(items).toBeDefined();

    if (items.length <= 1) {
      console.info(`Only ${items.length} record(s) found - sorting verification not applicable`);
      return;
    }

    for (let i = 1; i < items.length; i++) {
      expect(items[i - 1].DateCreated).toBeDefined();
      expect(items[i].DateCreated).toBeDefined();

      const prevDate = new Date(items[i - 1].DateCreated);
      const currDate = new Date(items[i].DateCreated);

      expect(prevDate).toBeInstanceOf(Date);
      expect(currDate).toBeInstanceOf(Date);
      expect(prevDate.getTime()).not.toBeNaN();
      expect(currDate.getTime()).not.toBeNaN();
      expect(prevDate.getTime()).toBeGreaterThanOrEqual(currDate.getTime());
    }

    console.info(`Verified ${items.length} records are sorted in descending order by DateCreated`);
  });
});
