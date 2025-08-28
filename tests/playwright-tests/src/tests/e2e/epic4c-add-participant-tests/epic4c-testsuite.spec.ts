import test from "@playwright/test";
import { getNenmsSubscriberId, getRecordsFromBsSelectRetrieveCohort } from "../../../api/distributionService/bsSelectService";
import { composeValidators, expectStatus, validateResponseByStatus } from "../../../api/responseValidators";
import { getLatestValidDatefromDatabase } from "../../steps/steps";

test.describe('@regression @e2e @epic4c- Tests', () => {

  test.only('DTOSS-10011 @not-runner-based - Verify subscribtion IDs on Nems table', {
    annotation: [{
      type: 'Requirement - https://nhsd-jira.digital.nhs.uk/browse/DTOSS-3881',
      description: 'Tests - https://nhsd-jira.digital.nhs.uk/browse/DTOSS-10011',
    }]
  }, async ({ request }, testInfo) => {


    await test.step(`Check latest ID by date`, async () => {

      const response = await getNenmsSubscriberId(request);

      const genericValidations = composeValidators(
        expectStatus(200),
        validateResponseByStatus()
      );
      await genericValidations(response);


      // expect(response.data.length).toBe(ExpectedRowCount);
    });
  });
});
