import { test, expect } from '@playwright/test';
import { cleanupDatabase } from '../../database/sqlVerifier';
import { config } from '../../config/env'
import { createParquetFromJson } from '../../parquet/parquet-multiplier';
import { getApiTestData, processFileViaStorage, validateSqlDatabase } from '../steps/steps';

// Base URL of the API endpoint
const BASE_URL = config.endpointRetrieveCohortDistributionData


test.describe.serial.only('Positive - Cohort Distribution Data Retrieval API ADD and AMENDED', async () => {

  test('01 200 - @api @DTOSS-5928 @TC1_SIT Verify the ability to process CaaS test file with 10 records from Cohort Manager to BS Select (ADD)', async ({ request }, testInfo) => {

    const [checkInDatabase, inputParticipantRecord, nhsNumbers, testFilesPath] = await getApiTestData(testInfo.title);

    await cleanupDatabase(nhsNumbers);

    const parquetFile = await createParquetFromJson(nhsNumbers, inputParticipantRecord, testFilesPath);

    await test.step(`When 10 ADD participants are processed via storage`, async () => {
      await processFileViaStorage(parquetFile);
    });

    await test.step(`Then participants should be updated in the cohort ready to be picked up`, async () => {
      await validateSqlDatabase(checkInDatabase);
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
  test('02 200 - @api @DTOSS-5928 @TC3_SIT Verify the ability to process CaaS file with 10 records from Cohort Manager to BS Select (AMENDED)', async ({ request }, testInfo) => {
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
  test('03 204 - @api @DTOSS-5928 @TC12_SIT Verify that BS Select can NOT retrieve same record on second attempt (ADD)', async ({ request }) => {

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
  test('05 200 - @api @DTOSS-5928 @TC15_SIT Verify that BS Select can retrieve a request id for a retrieved cohort successfully (ADD) 10 at a time', async ({ request }, testInfo) => {

    const [checkInDatabase, inputParticipantRecord, nhsNumbers, testFilesPath] = await getApiTestData(testInfo.title);

    await cleanupDatabase(nhsNumbers);

    const parquetFile = await createParquetFromJson(nhsNumbers, inputParticipantRecord, testFilesPath);

    await test.step(`When 20 ADD participants are processed via storage`, async () => {
      await processFileViaStorage(parquetFile);
    });

    await test.step(`Then participants should be updated in the cohort ready to be picked up`, async () => {
      await validateSqlDatabase(checkInDatabase);
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

test.describe.serial.only('Negative - Cohort Distribution Data Retrieval API ADD and AMENDED', async () => {

  test('04 500 - @api @DTOSS-5928 @TC14_SIT Verify that an error message is displayed when BS Select attempts to retrieve an already retrieved cohort(ADD)', async ({ request }, testInfo) => {


    const [checkInDatabase, inputParticipantRecord, nhsNumbers, testFilesPath] = await getApiTestData(testInfo.title);

    await cleanupDatabase(nhsNumbers);


    const parquetFile = await createParquetFromJson(nhsNumbers, inputParticipantRecord, testFilesPath);

    await test.step(`When 10 ADD participants are processed via storage`, async () => {
      await processFileViaStorage(parquetFile);
    });



    await test.step(`Then participants should be updated in the cohort ready to be picked up`, async () => {
      await validateSqlDatabase(checkInDatabase);
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

