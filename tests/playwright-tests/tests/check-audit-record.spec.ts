import { test, expect } from '@playwright/test';


interface AuditInformation {
  responseJson: string;
  length: number;
  status: any;
}

const endpoint = "";
const endpointRetrieveCohortRequestAudit = endpoint + "";
const endpointRetrieveCohortDistributionData = endpoint + "";

test.only('Verify external API Audit Record Creation on access', async ({ request }) => {

  var currentAuditInformation = await getResponseDetails(request, endpointRetrieveCohortRequestAudit);

  const accessCohortDistributionData = await getResponseDetails(request, endpointRetrieveCohortDistributionData);
  expect(accessCohortDistributionData.status).toBe(204);  // Validate the status is 204

  let attempts = 0;
  let updatedAuditInformation: AuditInformation;

  do {
    updatedAuditInformation = await getResponseDetails(request, endpointRetrieveCohortRequestAudit);
    attempts++;
    console.log("Attempt: " + attempts);
  } while ((updatedAuditInformation.length == currentAuditInformation.length) && attempts<10);

  expect(updatedAuditInformation.length).toBe(currentAuditInformation.length + 1);


});


async function getResponseDetails(request, endpoint) {

  var responseJson = "NA";
  var length = 0;
  const response = await request.get(endpoint);

  if (response.status() != 204) {
    responseJson = await response.json();
    length = responseJson.length - 1;
  }
  const status = response.status();

  return {
    responseJson,
    length,
    status
  };
}
