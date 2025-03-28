import { test, expect } from '@playwright/test';
import ApiClient from '../../api/apiClient';
import { config } from '../../config/env';


interface AuditInformation {
  responseJson: string;
  length: number;
  status: any;
}

test.describe('Integration Tests - Audit', () => {
  test.only('Verify API Audit Record Creation on access', async ({ request }) => {
    const api = new ApiClient(request);

    var currentAuditInformation = await api.getResponseDetails(config.baseURL + config.endpointRetrieveCohortRequestAudit);

    const accessCohortDistributionData = await api.getResponseDetails(config.baseURL + config.endpointRetrieveCohortDistributionData);
    expect(accessCohortDistributionData.status).toBe(204);  // Validate the status is 204

    let attempts = 0;
    let updatedAuditInformation: AuditInformation;

    do {
      updatedAuditInformation = await api.getResponseDetails(config.baseURL + config.endpointRetrieveCohortRequestAudit);
      attempts++;
      console.log("Attempt: " + attempts);
    } while ((updatedAuditInformation.length == currentAuditInformation.length) && attempts < 10);

    expect(updatedAuditInformation.length).toBe(currentAuditInformation.length + 1);


  });

})




