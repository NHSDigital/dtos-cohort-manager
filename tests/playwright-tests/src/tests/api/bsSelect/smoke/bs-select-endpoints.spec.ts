import { test, expect } from '@playwright/test';
import { getRecordsFromBsSelectRetrieveAudit, getRecordsFromBsSelectRetrieveCohort } from '../../../../api/distributionService/bsSelectService';


test.describe.serial('@smoke @api @bs_select_api retrieve cohort tests', async () => {


  test('check retrieve cohort endpoint up and running', async ({ request }) => {

    const response = await getRecordsFromBsSelectRetrieveCohort(request, { rowCount: 10 });
    console.info(`Response Status: ${response.status}`);
    expect(response.status).toBeTruthy();
  });

});

test.describe.serial('@smoke @api @bs_select_api retrieve audit tests', async () => {


  test('check retrieve cohort audit endpoint up and running', async ({ request }) => {

    const response = await getRecordsFromBsSelectRetrieveAudit(request);
    console.info(`Response Status: ${response.status}`);
    expect(response.status).toBeTruthy();
  });
});


