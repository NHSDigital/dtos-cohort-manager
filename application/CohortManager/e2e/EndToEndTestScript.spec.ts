import { test, expect } from '@playwright/test';
import { BlobServiceClient, BlockBlobClient } from '@azure/storage-blob';
import { QueueClient } from '@azure/storage-queue';
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
    await delay(20000);

    const recordFromDB = await checkDatabase();
    var res = recordFromDB["nhs_number"] == '3112728165';
    expect(res).toBeTruthy();
});


test('Upload Add file to Azure Blob twice and expect it error in database', async () => {
    // Initialize the Azure Blob Service Client and get the container and blob clients
    const blobServiceClient = BlobServiceClient.fromConnectionString(connectionString);
    const containerClient = blobServiceClient.getContainerClient(containerName);
    const blobName = "ADD_1B8F53_-_CAAS_BREAST_SCREENING_COHORT.parquet";
    const blobClient: BlockBlobClient = containerClient.getBlockBlobClient(blobName);

    const path = require('path');
    const testfileName = path.join(__dirname, 'ADD_1B8F53_-_CAAS_BREAST_SCREENING_COHORT.parquet');

    
    await blobClient.uploadFile(testfileName);
    // upload same file again and expect to it cause an error in database
    await delay(10000);
    await blobClient.uploadFile(testfileName);


    await delay(10000);
    // Check that the cohort distribution has made it to the database via an HTTP API call.
    const error = await checkDatabaseForErrors();
    
    var res = error.find(x => x["RuleId"] == '47');
    expect(res != null || res != undefined).toBeTruthy()
});




test('Upload Add file to Azure Blob and expect it to error in database with error codes 36, 3645 and not go to cohort', async () => {
    // Initialize the Azure Blob Service Client and get the container and blob clients
    const blobServiceClient = BlobServiceClient.fromConnectionString(connectionString);
    const containerClient = blobServiceClient.getContainerClient(containerName);
    const blobName = "Exception_1B8F53_-_CAAS_BREAST_SCREENING_COHORT.parquet";
    const blobClient: BlockBlobClient = containerClient.getBlockBlobClient(blobName);

    const path = require('path');
    const testfileName = path.join(__dirname, 'Exception_1B8F53_-_CAAS_BREAST_SCREENING_COHORT.parquet');

    
    await blobClient.uploadFile(testfileName);
    // upload same file again and expect to it cause an error in database
    await delay(20000);
    // Check that the cohort distribution has made it to the database via an HTTP API call.
    const itemsFromDatabase = await checkDatabase();
    
    const error = await checkDatabaseForErrors();
    
    var Rule36InDatabase = error.find(x => x["RuleId"] == '36');
    var Rule3645InDatabase = error.find(x => x["RuleId"] == '3645');

    expect(Rule36InDatabase != null || Rule3645InDatabase != undefined).toBeTruthy()
    var res = itemsFromDatabase["nhs_number"] == '2612314172';
    expect(res).toBeFalsy()

});


async function checkDatabase(): Promise<[]> {
  const rowCount = 5;
  const url = `http://localhost:7095/api/RetrieveCohortDistributionData?rowCount=${rowCount}`;
  try {
    const response = await fetch(url);
    if (response.ok) {
        var responseJson = await response.json();
        let items = responseJson[0]
        return items;
    } else {
      console.error(`Error: ${response.status}`);
  
      return [];
    }
  }
  catch (error ) {
    return []
  }
}


async function checkDatabaseForErrors(): Promise<[]> {
  const url = `http://localhost:7070/api/GetValidationExceptions`;
  console.log("here");
  var response = await fetch(url) 
    try {
      if (response.ok) {
        var responseJson = await response.json();
        let items = responseJson.Items
        return items;

      } else {
        console.error(`Error: ${response.status}`);
        return [];
      }
    }
    catch(error) {
      return [];
    }
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