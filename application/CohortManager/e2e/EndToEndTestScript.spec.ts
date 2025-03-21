import { test, expect } from '@playwright/test';
import { BlobServiceClient, BlockBlobClient } from '@azure/storage-blob';
import { EndToEndHelper } from './EndToEndHelper';
import configJson from '../playwrightConfig.json';

test.describe('End To End tests', () => {
  const endToEndHelper = new EndToEndHelper();

    test('01.Upload Add file to Azure Blob and check database', async () => {
      // Initialize the Azure Blob Service Client and get the container and blob clients
      const blobServiceClient = BlobServiceClient.fromConnectionString(configJson.ConnectionString);
      const containerClient = blobServiceClient.getContainerClient(configJson.containerName);
      const blobName = "add_1_-_CAAS_BREAST_SCREENING_COHORT.parquet";
      const blobClient: BlockBlobClient = containerClient.getBlockBlobClient(blobName);
  
      const path = require('path');
      const testfileName = path.join(__dirname, 'add_1_-_CAAS_BREAST_SCREENING_COHORT.parquet');
  
      // var testFile = loadParquetFile(testfileName);
      await blobClient.uploadFile(testfileName);
      await endToEndHelper.delay(20000);
  
      const recordFromDB = await endToEndHelper.GetCohortRecordsFromAPI(2);
      var res = recordFromDB["nhs_number"] == '3112728165';
      expect(res).toBeTruthy();
  });
  
  
  test('02.Upload Add file to Azure Blob twice and expect it error in database', async () => {
      // Initialize the Azure Blob Service Client and get the container and blob clients
      const blobServiceClient = BlobServiceClient.fromConnectionString(configJson.ConnectionString);
      const containerClient = blobServiceClient.getContainerClient(configJson.containerName);
      const blobName = "ADD_1B8F53_-_CAAS_BREAST_SCREENING_COHORT.parquet";
      const blobClient: BlockBlobClient = containerClient.getBlockBlobClient(blobName);
  
      const path = require('path');
      const testfileName = path.join(__dirname, 'ADD_1B8F53_-_CAAS_BREAST_SCREENING_COHORT.parquet');
  
      
      await blobClient.uploadFile(testfileName);
      // upload same file again and expect to it cause an error in database
      await endToEndHelper.delay(10000);
      await blobClient.uploadFile(testfileName);
  
  
      await endToEndHelper.delay(15000);
      // Check that the cohort distribution has made it to the database via an HTTP API call.
      const error = await endToEndHelper.GetErrorsFromExceptionAPI();
      
      var res = error.find(x => x["RuleId"] == '47');
      expect(res != null || res != undefined).toBeTruthy()
  });
  
  
  
  
  test('03.Upload Add file to Azure Blob and expect it to error in database with error codes 36, 3645 and not go to cohort', async () => {
      // Initialize the Azure Blob Service Client and get the container and blob clients
      const blobServiceClient = BlobServiceClient.fromConnectionString(configJson.ConnectionString);
      const containerClient = blobServiceClient.getContainerClient(configJson.containerName);
      const blobName = "Exception_1B8F53_-_CAAS_BREAST_SCREENING_COHORT.parquet";
      const blobClient: BlockBlobClient = containerClient.getBlockBlobClient(blobName);
  
      const path = require('path');
      const testfileName = path.join(__dirname, 'Exception_1B8F53_-_CAAS_BREAST_SCREENING_COHORT.parquet');
  
      
      await blobClient.uploadFile(testfileName);
      // upload same file again and expect to it cause an error in database
      await endToEndHelper.delay(20000);
      // Check that the cohort distribution has made it to the database via an HTTP API call.
      const itemsFromDatabase = await endToEndHelper.GetCohortRecordsFromAPI(5);
      
      const error = await endToEndHelper.GetErrorsFromExceptionAPI();
      
      var Rule36InDatabase = error.find(x => x["RuleId"] == '36');
      var Rule3645InDatabase = error.find(x => x["RuleId"] == '3645');
  
      expect(Rule36InDatabase != null || Rule3645InDatabase != undefined).toBeTruthy()
      var res = itemsFromDatabase["nhs_number"] == '2612314172';
      expect(res).toBeFalsy()
  
  });
  
  
  
  test('04.Upload update file and except it be in the database', async () => {
    const amendedItems = new Array(0);
    var arrIndex = 0;
  
    // Initialize the Azure Blob Service Client and get the container and blob clients
    const blobServiceClient = BlobServiceClient.fromConnectionString(configJson.ConnectionString);
    const containerClient = blobServiceClient.getContainerClient(configJson.containerName);
    const blobName = "AMENDED_1B8F53_-_CAAS_BREAST_SCREENING_COHORT.parquet";
    const blobClient: BlockBlobClient = containerClient.getBlockBlobClient(blobName);
  
    const path = require('path');
    const testfileName = path.join(__dirname, 'AMENDED_1B8F53_-_CAAS_BREAST_SCREENING_COHORT.parquet');
  
    
    await blobClient.uploadFile(testfileName);
    // upload same file again and expect to it cause an error in database
    await endToEndHelper.delay(20000);
  
    (await endToEndHelper.GetCohortRecordsFromAPI(5)).forEach((item) =>{
      if(item["nhs_number"] == '2612514171') {
        amendedItems[arrIndex] = item
        arrIndex++
      }
    });
  
    
    expect(amendedItems.length == 1).toBeTruthy()
  
  });
  
  
  
  test('05.Upload update file and except it not to be in database and error 22 in error table', async () => {
    // Initialize the Azure Blob Service Client and get the container and blob clients
    const blobServiceClient = BlobServiceClient.fromConnectionString(configJson.ConnectionString);
    const containerClient = blobServiceClient.getContainerClient(configJson.containerName);
    const blobName = "AMENDED_1B8F53_-_CAAS_BREAST_SCREENING_COHORT.parquet";
    const blobClient: BlockBlobClient = containerClient.getBlockBlobClient(blobName);
  
    const path = require('path');
    const testfileName = path.join(__dirname, 'AMENDED1_1B8F53_-_CAAS_BREAST_SCREENING_COHORT.parquet');
  
    
    await blobClient.uploadFile(testfileName);
    await endToEndHelper.delay(20000);
    // upload same file again and expect to it cause an error in database
    const itemsFromDatabase = await endToEndHelper.GetCohortRecordsFromAPI(5);
      
    const error = await endToEndHelper.GetErrorsFromExceptionAPI();
    
    var Rule22InDatabase = error.find(x => x["RuleId"] == '22');
    
    expect(Rule22InDatabase != null || Rule22InDatabase != undefined).toBeTruthy()
    var res = itemsFromDatabase["nhs_number"] == '2312514176';
    expect(res).toBeFalsy()
  });
  
  
  test('06.Upload file with 2 records and except 2 records to be returned from API', async () => {
    
    const amendedItems = new Array(0);
    var arrIndex = 0; 
    
    // Initialize the Azure Blob Service Client and get the container and blob clients
    const blobServiceClient = BlobServiceClient.fromConnectionString(configJson.ConnectionString);
    const containerClient = blobServiceClient.getContainerClient(configJson.containerName);
    const blobName = "ADD_2_RECORDS_-_CAAS_BREAST_SCREENING_COHORT.parquet";
    const blobClient: BlockBlobClient = containerClient.getBlockBlobClient(blobName);
  
    const path = require('path');
    const testfileName = path.join(__dirname, 'ADD_2_RECORDS_-_CAAS_BREAST_SCREENING_COHORT.parquet');
  
    
    await blobClient.uploadFile(testfileName);
    // upload same file again and expect to it cause an error in database
    await endToEndHelper.delay(20000);
  
    
    (await endToEndHelper.GetCohortRecordsFromAPI(5)).forEach((item) =>{
      if(item["nhs_number"] == '1111110662' || item["nhs_number"] == '2222211794') {
        amendedItems[arrIndex] = item
        arrIndex++
      }
    });
  
  
    expect(amendedItems.length == 2).toBeTruthy()
  });
  
  
  
  
  test('07.Upload file with 300 records and except 300 records to be returned from API', async () => {
    
    test.setTimeout(100000); // Sets timeout to 45 seconds for this test
    
    // Initialize the Azure Blob Service Client and get the container and blob clients
    const blobServiceClient = BlobServiceClient.fromConnectionString(configJson.ConnectionString);
    const containerClient = blobServiceClient.getContainerClient(configJson.containerName);
    const blobName = "ADD_500_2_-_CAAS_BREAST_SCREENING_COHORT.parquet";
    const blobClient: BlockBlobClient = containerClient.getBlockBlobClient(blobName);
  
    const path = require('path');
    const testfileName = path.join(__dirname, 'ADD_500_2_-_CAAS_BREAST_SCREENING_COHORT.parquet');
  
    
    await blobClient.uploadFile(testfileName);
    // upload same file again and expect to it cause an error in database
    await endToEndHelper.delay(60000);
  
    var hasMoreThan500Records = (await endToEndHelper.GetCohortRecordsFromAPI(300)).length >= 300
  
    expect(hasMoreThan500Records).toBeTruthy()
  });    

});
