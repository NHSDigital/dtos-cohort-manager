import { test, expect } from '@playwright/test';
import { config } from '../../../../config/env'


test.describe.serial('@smoke @api @external_bs_select_api retrieve cohort tests', async () => {

  const BASE_URL = config.endpointExternalBsSelectRetrieveCohortDistributionData
  const endpoint = `${BASE_URL}api/RetrieveCohortDistributionData`

  test('@ut check retrieve cohort endpoint up and running', async ({ request }) => {

    const response = await request.get(`${endpoint}`, {
      params: {
        rowCount: 10
      }
    });
    console.info(`Response Status: ${response.status()}`);
    expect(response.status()).toBeTruthy();
  });

});

test.describe.serial('@smoke @api @external_bs_select_api retrieve audit tests', async () => {

  const BASE_URL = config.endpointExternalBsSelectRetrieveCohortRequestAudit;
  const endpoint = `${BASE_URL}api/RetrieveCohortRequestAudit`

  test('@ut check retrieve cohort audit endpoint up and running', async ({ request }) => {
    const response = await request.get(`${endpoint}`);
    console.info(`Response Status: ${response.status()}`);
    expect(response.status()).toBeTruthy();
  });
});


