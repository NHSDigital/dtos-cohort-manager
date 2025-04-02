import * as fs from 'fs';
import { BlobServiceClient } from "@azure/storage-blob";
import path from 'path';
import { config } from '../config/env'

export async function uploadToLocalStorage(filePath: string): Promise<void> {
  const connectionString = config.azureConnectionString;
  const containerName = config.containerName;
  const blobServiceClient = BlobServiceClient.fromConnectionString(connectionString);
  const containerClient = blobServiceClient.getContainerClient(containerName);
  const blobName = path.basename(filePath);
  const blockBlobClient = containerClient.getBlockBlobClient(blobName);
  const fileStream = fs.createReadStream(filePath);

  try {
    const uploadBlobResponse = await blockBlobClient.uploadStream(fileStream);
    console.info(`Uploaded file to Azure Blob Storage: ${blobName}`);
    console.info(`Response: ${uploadBlobResponse.requestId}`);
  } catch (error) {
    console.error('Error uploading file to Azure Blob Storage:', error);
  }
}
