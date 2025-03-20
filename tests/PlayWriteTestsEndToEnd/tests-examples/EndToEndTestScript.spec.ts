import { test, expect } from '@playwright/test';
import { BlobServiceClient, BlockBlobClient } from '@azure/storage-blob';
import { QueueClient } from '@azure/storage-queue';
import parquetjs from '@dsnp/parquetjs';
import parquet from '@dsnp/parquetjs';
import { read } from 'fs';

// Replace with your actual connection string
const connectionString = "";
const containerName = "inbound";
const queueName = "add-participant-queue";

test('Upload Add file to Azure Blob and check database', async () => {
    // Initialize the Azure Blob Service Client and get the container and blob clients
    const blobServiceClient = BlobServiceClient.fromConnectionString(connectionString);
    const containerClient = blobServiceClient.getContainerClient(containerName);
    const blobName = "add_1_-_CAAS_BREAST_SCREENING_COHORT.parquet";
    const blobClient: BlockBlobClient = containerClient.getBlockBlobClient(blobName);

    const path = require('path');
    const testfileName = path.join(__dirname, 'add_1_-_CAAS_BREAST_SCREENING_COHORT.parquet');

    // var testFile = loadParquetFile(testfileName);
    await blobClient.uploadFile(testfileName);
    await delay(5000);

    const isInDatabase = await checkDatabase();
    expect(isInDatabase).toBeTruthy();
});


test('Upload Add file to Azure Blob twice and expect it error in database', async () => {
  // Initialize the Azure Blob Service Client and get the container and blob clients
  const blobServiceClient = BlobServiceClient.fromConnectionString(connectionString);
  const containerClient = blobServiceClient.getContainerClient(containerName);
  const blobName = "add_1_-_CAAS_BREAST_SCREENING_COHORT.parquet";
  const blobClient: BlockBlobClient = containerClient.getBlockBlobClient(blobName);

  const path = require('path');
  const testfileName = path.join(__dirname, 'add_1_-_CAAS_BREAST_SCREENING_COHORT.parquet');

  
  await blobClient.uploadFile(testfileName);
  // upload same file again and expect to it cause an error in database
  await delay(5000);
  await blobClient.uploadFile(testfileName);


  await delay(5000);
  // Check that the cohort distribution has made it to the database via an HTTP API call.
  const isInDatabase = await checkDatabase();
  expect(isInDatabase).toBeTruthy();
  const error = await checkDatabaseForErrors();
  var parsedJsonData = JSON.parse(await error);
  
  expect(parsedJsonData.find(CallbackFunctionToFindRuleById(parsedJsonData, '47')).RuleDescription == "ParticipantMustNotExist").toBeTruthy()
});

function CallbackFunctionToFindRuleById(rule, ruleId) {
  return rule.RuleId === ruleId;
}


async function checkDatabase(): Promise<boolean> {
  const rowCount = 1;
  const url = `http://localhost:7095/api/RetrieveCohortDistributionData?rowCount=${rowCount}`;
  
  const response = await fetch(url);
  if (response.ok) {
    const responseData = await response.text();

    return responseData !== "";
  } else {
    console.error(`Error: ${response.status}`);

    return false;
  }
}


async function checkDatabaseForErrors(): Promise<string> {
  const url = `http://localhost:7070/api/GetValidationExceptions`;
  console.log("here");
  await fetch(url).then((response) => { 
    try {
      if (response.ok) {
        return response.json()

      } else {
        console.error(`Error: ${response.status}`);
        return "";
      }
    }
    catch(error) {
      return error;
    }
  });
  return "";
}



async function retrieveNextMessage(queueClient: QueueClient): Promise<boolean> {
  const exists = await queueClient.exists();
  if (exists) {
    const properties = await queueClient.getProperties();

    if (properties.approximateMessagesCount && properties.approximateMessagesCount > 0) {
      const receiveResponse = await queueClient.receiveMessages({ numberOfMessages: 1 });

      if (receiveResponse.receivedMessageItems.length > 0) {
        
        const message = receiveResponse.receivedMessageItems[0];
        const messageText = message.messageText;

        await queueClient.deleteMessage(message.messageId, message.popReceipt);
        return messageText !== "";
      }
    }
  }
  return false;
}

const delay = ms => new Promise(res => setTimeout(res, ms));