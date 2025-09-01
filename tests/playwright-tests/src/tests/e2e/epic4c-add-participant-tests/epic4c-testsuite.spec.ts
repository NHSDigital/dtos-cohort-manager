import { expect, test } from '../../fixtures/test-fixtures';
import { getRecordsFromBsSelectRetrieveAudit, getRecordsFromBsSelectRetrieveCohort, getRecordsFromNemsSubscription } from "../../../api/distributionService/bsSelectService";
import { composeValidators, expectStatus, validateResponseByStatus } from "../../../api/responseValidators";
import { getLatestValidDatefromDatabase } from "../../steps/steps";

test.describe('@regression @e2e @epic4c- Tests', () => {

  test.only('DTOSS-10011 @not-runner-based - Verify subscribtion IDs on Nems table', {
    annotation: [{
      type: 'Requirement - https://nhsd-jira.digital.nhs.uk/browse/DTOSS-3881',
      description: 'Tests - https://nhsd-jira.digital.nhs.uk/browse/DTOSS-10011',
    }]
  }, async ({ request }, testInfo) => {


    await test.step(`Then NEMS_SUBSCRIPTION should have subscriber ID`, async () => {

        const response = await getRecordsFromNemsSubscription(request);
        console.log(response);
        //const lastRecord = response.data[response.data.length - 1];
        //expect(lastRecord?.StatusCode).toBe("200");
    });
  });
});
