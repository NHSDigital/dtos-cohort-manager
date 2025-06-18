import { test, expect } from '@playwright/test';
import { getApiTestData} from '../../../steps/steps';
import { composeValidators, expectStatus} from '../../../../api/responseValidators';
import { retrieveDemographicPDS } from '../../../../api/distributionService/bsSelectService';
import patientData from './test-data/complete-patient.json';
import http from 'http';


let server: http.Server;

test.beforeAll(async () => {
  server = http.createServer((req, res) => {
    if (req.method === 'GET') {
      res.writeHead(200, { 'Content-Type': 'application/json' });
      res.end(
          JSON.stringify(patientData)
        );
    } else {
      res.writeHead(404);
      res.end();
    }
  });

  server.listen(3000);
});

test.afterAll(() => {
  server.close();
});

test.describe.serial('@api get Participant data from PDS function', () => {
  

  test('@DTOSS-7772-01 - Verify participant is successfully gotten from PDS API', async ({ request }, testInfo) => {
    const [,,nhsNumbers,] = await getApiTestData(testInfo.title);

    await test.step(`call PDS function`, async () => {
      const payload = {
        NhsNumber: nhsNumbers[0],
      };

      const response = await retrieveDemographicPDS(request, payload);

      const validators = composeValidators(
        expectStatus(200)
      );
      await validators(response);

      const lastResponse = await retrieveDemographicPDS(request, payload);
      expect(lastResponse.status).toBe(200);
    });
  });

   /*test('@DTOSS-7772-01 - Verify participant is successfully gotten from PDS API', async ({ request }, testInfo) => {
    const [,,nhsNumbers,] = await getApiTestData(testInfo.title);

    await test.step(`call PDS function`, async () => {
      const payload = {
        NhsNumber: nhsNumbers[0],
      };

      const response = await retrieveDemographicPDS(request, payload);

      const validators = composeValidators(
        expectStatus(200)
      );
      await validators(response);

      const lastResponse = await retrieveDemographicPDS(request, payload);
      expect(lastResponse.status).toBe(200);
    });
  });*/
});

