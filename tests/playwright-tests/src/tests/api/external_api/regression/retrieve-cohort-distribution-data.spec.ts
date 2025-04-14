import { test, expect } from '@playwright/test';
import { config } from '../../../../config/env'
import { createParquetFromJson } from '../../../../parquet/parquet-multiplier';
import { getApiTestData, processFileViaStorage, cleanupDatabaseFromAPI, validateSqlDatabaseFromAPI } from '../../../steps/steps';

const BASE_URL = config.endpointRetrieveCohortDistributionData


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
    });


    await test.step(`And participants received from api should be 10 with status code of 200`, async () => {
      const rowCount = 10;
      const response = await request.get(`${BASE_URL}`, {
        params: {
          rowCount: 10
        }
      });

      expect(response.status()).toBe(200);

      const responseBody = await response.json();
      expect(Array.isArray(responseBody)).toBe(true);
      expect(responseBody.length).toBe(rowCount);
    });


  });
  test('@DTOSS-5928-02 200 - @TC3_SIT Verify the ability to process CaaS file with 10 records from Cohort Manager to BS Select (AMENDED)', async ({ request }, testInfo) => {
    console.info('Running test: ', testInfo.title);
    const [inputParticipantRecord, nhsNumbers, testFilesPath] = await getApiTestData(testInfo.title);

    const parquetFile = await createParquetFromJson(nhsNumbers, inputParticipantRecord, testFilesPath, "AMENDED");
    await test.step(`When AMENDED ADD participants are processed via storage for already Added participants`, async () => {
      await processFileViaStorage(parquetFile);
    });

    await test.step(`Then participants received from api should be 10 with status code of 200`, async () => {
      const expectedRowCount = 10;
      const response = await request.get(`${BASE_URL}`, {
        params: {
          rowCount: 100
        }
      });

      expect(response.status()).toBe(200);

      const responseBody = await response.json();
      expect(Array.isArray(responseBody)).toBe(true);
      expect(responseBody.length).toBe(expectedRowCount);
    });

  });
  test('@DTOSS-5928-03 204 - @TC12_SIT Verify that BS Select can NOT retrieve same record on second attempt (ADD)', async ({ request }) => {

    await test.step(`Then no participants should be received with status code of 204`, async () => {
      const response = await request.get(`${BASE_URL}`, {
        params: {
          rowCount: 100
        }
      });

      expect(response.status()).toBe(204);
      const responseBody = await response.text();
      expect(responseBody).toBe('');
    });


  });
  test('@DTOSS-5928-05 200 - @TC15_SIT Verify that BS Select can retrieve a request id for a retrieved cohort successfully (ADD) 10 at a time', async ({ request }, testInfo) => {

    const [checkInDatabase, inputParticipantRecord, nhsNumbers, testFilesPath] = await getApiTestData(testInfo.title);

    await cleanupDatabaseFromAPI(request, nhsNumbers);

    const parquetFile = await createParquetFromJson(nhsNumbers, inputParticipantRecord, testFilesPath);

    await test.step(`When 20 ADD participants are processed via storage`, async () => {
      await processFileViaStorage(parquetFile);
    });

    await test.step(`Then participants should be updated in the cohort ready to be picked up`, async () => {
      await validateSqlDatabaseFromAPI(request, checkInDatabase);
    });


    await test.step(`And participants received from api should be 10 with status code of 200`, async () => {
      const rowCount = 10;
      const response = await request.get(`${BASE_URL}`, {
        params: {
          rowCount: 10
        }
      });

      expect(response.status()).toBe(200);

      const responseBody = await response.json();
      expect(Array.isArray(responseBody)).toBe(true);
      expect(responseBody.length).toBe(rowCount);
    });

    await test.step(`And on 2nd hit the remaining 10 participants should be received from api with status code of 200`, async () => {
      const expectedRowCount = 10;
      const response = await request.get(`${BASE_URL}`, {
        params: {
          rowCount: 10
        }
      });

      expect(response.status()).toBe(200);

      const responseBody = await response.json();
      expect(Array.isArray(responseBody)).toBe(true);
      expect(responseBody.length).toBe(expectedRowCount);
    });

    await test.step(`And on 3rd hit the no participants should be received from api with status code of 204`, async () => {
      const response = await request.get(`${BASE_URL}`, {
        params: {
          rowCount: 10
        }
      });

      expect(response.status()).toBe(204);
      const responseBody = await response.text();
      expect(responseBody).toBe('');

    });

  });

});

test.describe.serial('@regression @api Negative - Cohort Distribution Data Retrieval API ADD and AMENDED', async () => {

  test('@DTOSS-5928-04 500 - @TC14_SIT Verify that an error message is displayed when BS Select attempts to retrieve an already retrieved cohort(ADD)', async ({ request }, testInfo) => {


    const [checkInDatabase, inputParticipantRecord, nhsNumbers, testFilesPath] = await getApiTestData(testInfo.title);

    await cleanupDatabaseFromAPI(request, nhsNumbers);


    const parquetFile = await createParquetFromJson(nhsNumbers, inputParticipantRecord, testFilesPath);

    await test.step(`When 10 ADD participants are processed via storage`, async () => {
      await processFileViaStorage(parquetFile);
    });



    await test.step(`Then participants should be updated in the cohort ready to be picked up`, async () => {
      await validateSqlDatabaseFromAPI(request, checkInDatabase);
    });


    await test.step(`And Internal server error should be received with status code 500`, async () => {

      const requestIdNotExists = '81b723eb-8b40-46bc-84dd-2459c22d69be';
      const response = await request.get(`${BASE_URL}`, {
        params: {

          requestId: requestIdNotExists
        }
      });

      expect(response.status()).toBe(500);
      const responseBody = await response.text();
      expect(responseBody).toBe('');
    });

  });


});

