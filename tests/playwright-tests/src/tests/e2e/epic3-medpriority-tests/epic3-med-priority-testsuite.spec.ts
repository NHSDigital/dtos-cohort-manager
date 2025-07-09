import { getRecordsFromBsSelectRetrieveAudit, getRecordsFromBsSelectRetrieveCohort } from '../../../api/distributionService/bsSelectService';
import { composeValidators, expectStatus, validateResponseByStatus } from '../../../api/responseValidators';
import { expect, test } from '../../fixtures/test-fixtures';
import { validateSqlDatabaseFromAPI } from "../../steps/steps";
import { getRecordsFromCohortDistributionService } from '../../../api/dataService/cohortDistributionService';
import { TestHooks } from '../../hooks/test-hooks';


test.describe('@regression @e2e @epic3-med-priority Tests', () => {

  TestHooks.setupAddTestHooks();
  test('@DTOSS-5561-01 @not-runner-based @bs-select - CohortDistribution_Requesting data from Cohort Manager and set record to extracted and add the request ID to the data table', {
    annotation: {
      type: 'Requirement',
      description: 'DTOSS-3650',
    },
  }, async ({ request, testData }) => {

    await test.step('Then processed ADD participant should be received using bs select get request where IsExtracted = 0', async () => {
      await validateSqlDatabaseFromAPI(request, testData.checkInDatabase);
      //check all records in the cohort to be set as 0
      const cohortResponse = await getRecordsFromCohortDistributionService(request);
      cohortResponse.data.forEach((record: { IsExtracted: number; }) => {
        expect(record.IsExtracted).toBe(0);
      });
      const ExpectedRowCount = 20;

      const retrieveCohortBSSelectResponse = await getRecordsFromBsSelectRetrieveCohort(request, { rowCount: 20, screeningServiceId: 1 });
      expect(retrieveCohortBSSelectResponse.data.length).toBe(ExpectedRowCount);

    });

    await test.step('And IsExtracted flag is set to 1', async () => {
      const cohortResponse = await getRecordsFromCohortDistributionService(request);
      cohortResponse.data.forEach((record: { IsExtracted: number; }) => {
        expect(record.IsExtracted).toBe(1);
      });
    });

    await test.step('And RequestId is assigned UUID v4 format to all fetched records', async () => {
      const cohortResponse = await getRecordsFromCohortDistributionService(request);
      cohortResponse.data.forEach((record: { RequestId: string | undefined; }) => {
        expect(record.RequestId).toBeDefined();
        expect(record.RequestId).toMatch(/^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i);
      });
    });
  });

  test('@DTOSS-5562-01 @not-runner-based @bs-select - CohortDistribution Requesting data from Cohort Manager with incorrect parameters - RowCount as empty', {
    annotation: [{
      type: 'Requirement',
      description: 'DTOSS-3650',
    }]
  }, async ({ request, testData }) => {

    await test.step('And ADD participant is processed with IsExtracted = 0', async () => {
      await validateSqlDatabaseFromAPI(request, testData.checkInDatabase);

    });

    await test.step('When Retrieve Cohort BS Select API returns available records with status 200, with RowCount as empty', async () => {
      const response = await getRecordsFromBsSelectRetrieveCohort(request, { rowCount: ``, screeningServiceId: 1 });
      const genericValidations = composeValidators(
        expectStatus(200),
        validateResponseByStatus()
      );
      await genericValidations(response);
    });

    await test.step('Then BS_SELECT_REQUEST_AUDIT should have an entry for 200', async () => {
      const response = await getRecordsFromBsSelectRetrieveAudit(request);
      const lastRecord = response.data[response.data.length - 1];
      expect(lastRecord?.StatusCode).toBe("200");
    });

  });

});



