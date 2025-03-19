import { test, expect } from '@playwright/test';
import { BlobServiceClient, BlockBlobClient } from '@azure/storage-blob';
import { QueueClient } from '@azure/storage-queue';
import parquetjs from '@dsnp/parquetjs';
import parquet from '@dsnp/parquetjs';

// Replace with your actual connection string
const connectionString = "";
const containerName = "inbound";
const queueName = "add-participant-queue";

test('Upload file to Azure Blob and check database', async () => {
    // Initialize the Azure Blob Service Client and get the container and blob clients
    const blobServiceClient = BlobServiceClient.fromConnectionString(connectionString);
    const containerClient = blobServiceClient.getContainerClient(containerName);
    const blobName = "add_1_-_CAAS_BREAST_SCREENING_COHORT.parquet";
    const blobClient: BlockBlobClient = containerClient.getBlockBlobClient(blobName);

    // Determine the file path. Here we assume the file is in the project root.
    // Adjust the path if your file is stored elsewhere.
    let file = "add_1_-_CAAS_BREAST_SCREENING_COHORT.parquet"

      var rawFile = new XMLHttpRequest();
      rawFile.open("GET", file, false);
      rawFile.onreadystatechange = async function ()
      { 
          if(rawFile.readyState === 4)
          {
              if(rawFile.status === 200 || rawFile.status == 0)
              { 
                  var allText = rawFile.responseText;
                  await blobClient.upload(allText, allText.length);
              }
          }
      }
      rawFile.send(null);
    

    // Check that the cohort distribution has made it to the database via an HTTP API call.
    const isInDatabase = await checkDatabase();
    expect(isInDatabase).toBeTruthy();
});


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
