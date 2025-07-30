import { test, expect } from '@playwright/test';
import { createParquetFromJson } from '../../../../parquet/parquet-multiplier';
import { getApiTestData, processFileViaStorage, cleanupDatabaseFromAPI, validateSqlDatabaseFromAPI } from '../../../steps/steps';
import { checkMappingsByIndex } from '../../../../api/apiHelper';
import { getRecordsFromBsSelectRetrieveCohort } from '../../../../api/distributionService/bsSelectService'
import { composeValidators, expectStatus, validateResponseByStatus } from '../../../../api/responseValidators';
import { config } from "../../../../config/env";


test.describe.serial('@regression @api Positive - Cohort Distribution Data Retrieval API ADD and AMENDED', async () => {

  test('@DTOSS-5928-01 200 - @TC1_SIT Verify the ability to process CaaS test file with 10 records from Cohort Manager to BS Select (ADD)', async ({ request }, testInfo) => {

    const [checkInDatabase, inputParticipantRecord, nhsNumbers, testFilesPath] = await getApiTestData(testInfo.title);

    await cleanupDatabaseFromAPI(request, nhsNumbers);

    const parquetFile = await createParquetFromJson(nhsNumbers, inputParticipantRecord, testFilesPath);

    await test.step(`When 10 ADD participants are processed via storage`, async () => {
      await processFileViaStorage(parquetFile);
    });

    await test.step(`Then participants should be updated in the cohort ready to be picked up`, async () => {
      await validateSqlDatabaseFromAPI(request, checkInDatabase);
      await new Promise(resolve => setTimeout(resolve, config.apiWaitTime));
    });


    await test.step(`And participants received from api should be 10 with status code of 200`, async () => {
      const ExpectedRowCount = 10;

      const response = await getRecordsFromBsSelectRetrieveCohort(request, { rowCount: 10 });

      const genericValidations = composeValidators(
        expectStatus(200),
        validateResponseByStatus()
      );
      await genericValidations(response);

      //Extend custom assertions
      expect(Array.isArray(response.data)).toBeTruthy();
      expect(response.data.length).toBe(ExpectedRowCount);
    });


  });
  test('@DTOSS-5930-01 200 - @TC3_SIT Verify the ability to process CaaS file with 10 records from Cohort Manager to BS Select (AMENDED)', async ({ request }, testInfo) => {

    const [checkInDatabase, inputParticipantRecord, nhsNumbers, testFilesPath] = await getApiTestData(testInfo.title, "AMENDED");

    const parquetFile = await createParquetFromJson(nhsNumbers, inputParticipantRecord, testFilesPath, "AMENDED");
    await test.step(`When AMENDED ADD participants are processed via storage for already Added participants`, async () => {
      await processFileViaStorage(parquetFile);
    });

    await test.step(`Then participants should be updated in the cohort ready to be picked up`, async () => {
      await validateSqlDatabaseFromAPI(request, checkInDatabase);
      await new Promise(resolve => setTimeout(resolve, config.apiWaitTime));
    });

    await test.step(`Then participants received from api should be 10 with status code of 200`, async () => {
      const expectedRowCount = 10;

      const response = await getRecordsFromBsSelectRetrieveCohort(request, { rowCount: 10 });

      const genericValidations = composeValidators(
        expectStatus(200),
        validateResponseByStatus()
      );
      await genericValidations(response);

      //Extend custom assertions
      expect(Array.isArray(response.data)).toBe(true);
      expect(response.data.length).toBe(expectedRowCount);
    });

  });
  test('@DTOSS-5939-01 204 - @TC12_SIT Verify that BS Select can NOT retrieve same record on second attempt (ADD)', async ({ request }) => {

    await test.step(`Then no participants should be received with status code of 204`, async () => {

      const response = await getRecordsFromBsSelectRetrieveCohort(request, { rowCount: 10 });

      const genericValidations = composeValidators(
        expectStatus(204),
        validateResponseByStatus()
      );
      await genericValidations(response);

      //Extend custom assertions
    });


  });
  test('@DTOSS-5942-01 200 - @TC15_SIT Verify that BS Select can retrieve a request id for a retrieved cohort successfully (ADD) 10 at a time', async ({ request }, testInfo) => {

    const [checkInDatabase, inputParticipantRecord, nhsNumbers, testFilesPath] = await getApiTestData(testInfo.title);

    await cleanupDatabaseFromAPI(request, nhsNumbers);

    const parquetFile = await createParquetFromJson(nhsNumbers, inputParticipantRecord, testFilesPath);

    await test.step(`When 20 ADD participants are processed via storage`, async () => {
      await processFileViaStorage(parquetFile);
    });

    await test.step(`Then participants should be updated in the cohort ready to be picked up`, async () => {
      await validateSqlDatabaseFromAPI(request, checkInDatabase);
      await new Promise(resolve => setTimeout(resolve, config.apiWaitTime));
    });


    await test.step(`And participants received from api should be 10 with status code of 200`, async () => {
      const expectedRowCount = 10;

      const response = await getRecordsFromBsSelectRetrieveCohort(request, { rowCount: 10 });

      const genericValidations = composeValidators(
        expectStatus(200),
        validateResponseByStatus()
      );
      await genericValidations(response);

      //Extend custom assertions
      expect(response.data.length).toBe(expectedRowCount);
    });

    await test.step(`And on 2nd hit the remaining 10 participants should be received from api with status code of 200`, async () => {
      const expectedRowCount = 10;
      const response = await getRecordsFromBsSelectRetrieveCohort(request, { rowCount: 10 });

      const genericValidations = composeValidators(
        expectStatus(200),
        validateResponseByStatus()
      );
      await genericValidations(response);

      //Extend custom assertions
      expect(response.data.length).toBe(expectedRowCount);
    });

    await test.step(`And on 3rd hit the no participants should be received from api with status code of 204`, async () => {

      const response = await getRecordsFromBsSelectRetrieveCohort(request, { rowCount: 10 });

      const genericValidations = composeValidators(
        expectStatus(204),
        validateResponseByStatus()
      );
      await genericValidations(response);

      //Extend custom assertions
    });

  });

  test('@DTOSS-5940-01 200 - TC13_SIT: Verify that BS Select can retrieve an already retrieved cohort successfully(ADD)', async ({ request }, testInfo) => {

    const [checkInDatabase, inputParticipantRecord, nhsNumbers, testFilesPath] = await getApiTestData(testInfo.title);

    await cleanupDatabaseFromAPI(request, nhsNumbers);

    const parquetFile = await createParquetFromJson(nhsNumbers, inputParticipantRecord, testFilesPath);

    await test.step(`When 10 ADD participants are processed via storage`, async () => {
      await processFileViaStorage(parquetFile);
    });

    await test.step(`Then participants should be updated in the cohort ready to be picked up`, async () => {
      await validateSqlDatabaseFromAPI(request, checkInDatabase);
      await new Promise(resolve => setTimeout(resolve, config.apiWaitTime));
    });


    await test.step(`Send a GET request via RetrieveCohortDistribution 5 times, and validate that req_id 1 nhs numbers are mapped to req_id 2 nhs numbers`, async () => {

      const requestIdsToNhsNumbers: { requestId: string; nhsNumber: string }[] = [];
      const requestIdsToNhsNumbersFromResponse: { requestId: string; nhsNumber: string }[] = [];

      for (let i = 0; i < 5; i++) {
        const response = await getRecordsFromBsSelectRetrieveCohort(request, { rowCount: 2 });

        const genericValidations = composeValidators(
          expectStatus(200),
          validateResponseByStatus()
        );
        await genericValidations(response);

        //Extend custom assertions

        const currentBatch = response.data.map((item: any) => {
          return {
            requestId: item.request_id,
            nhsNumber: item.nhs_number
          };
        });

        requestIdsToNhsNumbers.push(...currentBatch);
      }

      // 6th request
      const response = await getRecordsFromBsSelectRetrieveCohort(request, { rowCount: 2 });

      const genericValidations = composeValidators(
        expectStatus(204),
        validateResponseByStatus()
      );
      await genericValidations(response);

      //Extend custom assertions

      const uniqueRequestIds = Array.from(new Set(requestIdsToNhsNumbers.map(item => item.requestId)));

      for (let i = 0; i < uniqueRequestIds.length; i++) {
        const currentRequestId = uniqueRequestIds[i];
        const nextRequestId = uniqueRequestIds[i + 1];

        if (i === 0) {


          const response = await getRecordsFromBsSelectRetrieveCohort(request, { requestId: currentRequestId, rowCount: 2 });

          const genericValidations = composeValidators(
            expectStatus(200),
            validateResponseByStatus()
          );
          await genericValidations(response);

          const nhsNumbers = response.data.map((item: any) => item.nhs_number);
          expect(nhsNumbers.length).toBe(2);

          const currentBatch = response.data.map((item: any) => {
            return {
              requestId: currentRequestId,
              nhsNumber: item.nhs_number
            };
          });

          requestIdsToNhsNumbersFromResponse.push(...currentBatch);
        }

        if (nextRequestId) {


          const nextResponse = await getRecordsFromBsSelectRetrieveCohort(request, { requestId: nextRequestId, rowCount: 2 });


          if (nextResponse.status == 200) {
            expect(Array.isArray(nextResponse.data)).toBe(true);
            expect(nextResponse.data.length).toBe(2);

            const nextNhsNumbers = nextResponse.data.map((item: any) => item.nhs_number);
            expect(nextNhsNumbers.length).toBe(2);

            const batch = nextResponse.data.map((item: any) => {
              return {
                requestId: nextRequestId,
                nhsNumber: item.nhs_number
              };
            });

            requestIdsToNhsNumbersFromResponse.push(...batch);

          } else {
            expect(nextResponse.status).toBe(204);
          }
        }
      }

      const result = await checkMappingsByIndex(requestIdsToNhsNumbers, requestIdsToNhsNumbersFromResponse);
      console.info(`Overall check result: ${result ? "PASS" : "FAIL"}`);
      expect(result).toBeTruthy();

    });
  });

});

test.describe.serial('@regression @api Negative - Cohort Distribution Data Retrieval API ADD and AMENDED', async () => {

  test('@DTOSS-5941-01 400 - @TC14_SIT Verify that status code 400 is received when BS Select attempts to retrieve for non existent requestId', async ({ request }, testInfo) => {


    const [checkInDatabase, inputParticipantRecord, nhsNumbers, testFilesPath] = await getApiTestData(testInfo.title);

    await cleanupDatabaseFromAPI(request, nhsNumbers);


    const parquetFile = await createParquetFromJson(nhsNumbers, inputParticipantRecord, testFilesPath);

    await test.step(`When 10 ADD participants are processed via storage`, async () => {
      await processFileViaStorage(parquetFile);
    });



    await test.step(`Then participants should be updated in the cohort ready to be picked up`, async () => {
      await validateSqlDatabaseFromAPI(request, checkInDatabase);
      await new Promise(resolve => setTimeout(resolve, config.apiWaitTime));
    });


    await test.step(`And 400 status code should be received for non existent requestId`, async () => {

      const nonExistentRequestId = '81b723eb-8b40-46bc-84dd-2459c22d69be';

      const response = await getRecordsFromBsSelectRetrieveCohort(request, { requestId: nonExistentRequestId, rowCount: 1 });

      const genericValidations = composeValidators(
        expectStatus(400),
        validateResponseByStatus()
      );
      await genericValidations(response);

    });
  });

});


