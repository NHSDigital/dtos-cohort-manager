import { test, APIRequestContext, expect } from "@playwright/test";
import { checkBlobExists, uploadToLocalStorage } from "../../storage/azureStorage";
import { InputData, ParticipantRecord, ServiceNowRequestValidations } from "../../interface/InputData";
import { config } from "../../config/env";
import * as fs from 'fs';
import path from "path";
import { validateApiResponse } from "../../api/apiHelper";
import { cleanDataBaseUsingServices } from "../../api/dataService/dataServiceCleaner";
import { ensureNhsNumbersStartWith999 } from "../fixtures/testDataHelper";
import { receiveParticipantViaServiceNow } from "../../api/distributionService/bsSelectService";
import { WireMockResponse } from "../../interface/wiremock";


export async function cleanupDatabaseFromAPI(request: APIRequestContext, numbers: string[]) {
  return test.step(`Cleanup database using data services`, async () => {
    await cleanDataBaseUsingServices(numbers, request);
  });
}

function getWireMockUrl(): string {
  const wireMockUrl = config.wireMockUrl;

  if (wireMockUrl.length === 0)
  {
    throw new Error(`❌ WIREMOCK_URL environment variable is empty`);
  }

  return wireMockUrl;
}

export async function cleanupWireMock(request: APIRequestContext) {
  const wireMockUrl = getWireMockUrl();

  return test.step(`Cleanup WireMock`, async () => {
    await request.delete(wireMockUrl);
  });
}

export async function validateSqlDatabaseFromAPI(request: APIRequestContext, validations: any) {
  return test.step(`Validate database for assertions`, async () => {
    const { status, errorTrace } = await validateApiResponse(validations, request);
    if (!status) {
      throw new Error(`❌ Validation failed after ${config.apiRetry} attempts, please checks logs for more details: ${errorTrace}`);
    }
  });
}

export async function validateServiceNowRequestWithMockServer(request: APIRequestContext, validations: ServiceNowRequestValidations[]) {
  const wireMockUrl = getWireMockUrl();

  var response = await request.get(wireMockUrl);
  var body = await response.json() as WireMockResponse;

  return test.step(`Validate ServiceNow Requests with WireMock`, async () => {
    validations.forEach(validation => {
      const { caseNumber, messageType } = validation.validation;

      var request = body.requests.find(request => request.request.url.endsWith(caseNumber));

      if (!request)
      {
        throw new Error(`❌ Validation failed, request not found for: ${caseNumber}`);
      }

      console.info(`🚧 Validating ServiceNow Message Type ${messageType} was sent for ${caseNumber}`);

      switch (messageType) {
        case 1:
          expect(request.request.url == `/api/x_nhsd_intstation/nhs_integration/9c78f87c97912e10dd80f2df9153aff5/CohortCaseUpdate/${caseNumber}`).toBeTruthy();
          expect(JSON.parse(request.request.body)['needs_attention'] == true).toBeTruthy();
          break;
        case 2:
          expect(request.request.url == `/api/x_nhsd_intstation/nhs_integration/9c78f87c97912e10dd80f2df9153aff5/CohortCaseUpdate/${caseNumber}`).toBeTruthy();
          expect(JSON.parse(request.request.body)['needs_attention'] == false).toBeTruthy();
          break;
        case 3:
          expect(request.request.url == `/api/x_nhsd_intstation/nhs_integration/9c78f87c97912e10dd80f2df9153aff5/CohortCaseResolution/${caseNumber}`).toBeTruthy();
          break;
        default:
          throw new Error(`❌ Validation failed, unexpected message type: ${messageType}`);
      }

      console.info(`✅ Validation Complete - ServiceNow Message Type ${messageType} request was sent for ${caseNumber}`);
    });
  });
}

export async function processFileViaStorage(parquetFilePath: string) {
  return test.step(`Process file via Storage`, async () => {
    await uploadToLocalStorage(parquetFilePath);
  });
}


export async function sendParticipantViaSnowAPI(request: APIRequestContext,
  payload: ParticipantRecord ) {
  return test.step(`Process file via SnowAPI`, async () => {
    await receiveParticipantViaServiceNow(request,payload);
  });
}

export async function getTestData(scenarioFolderName: string
  , recordType: string = "ADD"
  , createParquetFile = false): Promise<[any, string[], string?, Record<string, any>?, string?]> { //TODO fix return type
  return test.step(`Creating Input Data from JSON file`, async () => {
    const testFilesPath = path.join(__dirname, `../`, `${config.e2eTestFilesPath}/${scenarioFolderName.substring(0, 14)}/`);
    const jsonFile = fs.readdirSync(testFilesPath).find(fileName => fileName.endsWith('.json') && fileName.startsWith(recordType));
    let parquetFile: string = "";
    if (createParquetFile) {
      console.info("Parquet file will be created from input JSON");
    } else {
      parquetFile = testFilesPath + jsonFile?.replace('.json', '.parquet');
    }

    const parsedData: InputData = JSON.parse(fs.readFileSync(testFilesPath + jsonFile, 'utf-8'));
    const inputParticipantRecord: Record<string, any> = parsedData.inputParticipantRecord;

    let nhsNumbers: string[] = [];
    if (parsedData.inputParticipantRecord.length !== parsedData.nhsNumbers.length) {
      console.info(`Input participant record length (${parsedData.inputParticipantRecord.length}) does not match NHS numbers length (${parsedData.nhsNumbers.length}). Using NHS numbers from parsed data nhsNumbers property to attempt multiply records when parquet file is created.`);
      nhsNumbers = parsedData.nhsNumbers;
    } else {
      nhsNumbers = parsedData.validations.map(item =>
        String(item.validations.NHSNumber || item.validations.NhsNumber)
      );
    }

    if (nhsNumbers.length === 0 || nhsNumbers[0] === '') {
      nhsNumbers = parsedData.nhsNumbers;
    }

    ensureNhsNumbersStartWith999(nhsNumbers);


    const uniqueNhsNumbers: string[] = Array.from(new Set(nhsNumbers));
    return [parsedData.validations, uniqueNhsNumbers, parquetFile, inputParticipantRecord, testFilesPath];
  });
}

export function getCheckInDataBaseValidations(scenarioFolderName: string
  , recordType: string = "ADD") {
  const testFilesPath = path.join(__dirname, `../`, `${config.e2eTestFilesPath}/${scenarioFolderName.substring(0, 14)}/`);
  const jsonFile = fs.readdirSync(testFilesPath).find(fileName => fileName.endsWith('.json') && fileName.startsWith(recordType));
  const parsedData: InputData = JSON.parse(fs.readFileSync(testFilesPath + jsonFile, 'utf-8'));
  return parsedData.validations;

}

export function getConsolidatedAllTestData(
  scenarioFolderName: string,
  recordType: string = "ADD"
) {
  const scenarioFolders = scenarioFolderName.split("|").map(name => name.trim());
  let testFilesPath: string = "";
  let allValidations: any[] = [];
  let allInputParticipantRecords: any[] = [];
  let allNhsNumbers: any[] = [];
  let allServiceNowRequestValidations: any[] = [];

  scenarioFolders.forEach(folder => {
    try {
      testFilesPath = path.join(__dirname, `../`, `${config.e2eTestFilesPath}/${folder.substring(0, 14)}/`);
      const jsonFiles = fs.readdirSync(testFilesPath).filter(fileName => fileName.endsWith('.json') && fileName.startsWith(recordType));
      jsonFiles.forEach(jsonFile => {
        const srcPath = path.join(testFilesPath, jsonFile);
        try {
          const parsedData: InputData = JSON.parse(fs.readFileSync(srcPath, 'utf-8'));
          allValidations = allValidations.concat(parsedData.validations).filter(element => element !== undefined);
          allServiceNowRequestValidations = allServiceNowRequestValidations.concat(parsedData.serviceNowRequestValidations).filter(element => element !== undefined);
          allNhsNumbers = allNhsNumbers.concat(parsedData.nhsNumbers);
          if (Array.isArray(parsedData.inputParticipantRecord)) {
            allInputParticipantRecords = allInputParticipantRecords.concat(parsedData.inputParticipantRecord);
          } else if (parsedData.inputParticipantRecord) {
            allInputParticipantRecords.push(parsedData.inputParticipantRecord);
          }
        } catch (jsonErr) {
          console.error(`Failed to parse JSON file: ${srcPath}`, jsonErr);
        }
      });
    } catch (fsErr) {
      console.error(`Failed to read directory: ${testFilesPath}`, fsErr);
    }
  });

  return {
    validations: allValidations,
    inputParticipantRecords: allInputParticipantRecords,
    nhsNumbers: allNhsNumbers,
    serviceNowRequestValidations: allServiceNowRequestValidations,
    testFilesPath
  };
}

export async function getApiTestData(scenarioFolderName: string, recordType: string = "ADD"): Promise<any> { //TODO fix return type
  return test.step(`Creating Input Data from JSON file`, async () => {
    console.info('🏃‍♂️‍➡️\tRunning test For: ', scenarioFolderName);
    const testFilesPath = path.join(__dirname, `../`, `${config.apiTestFilesPath}/${scenarioFolderName.substring(0, 14)}/`);
    const jsonFile = fs.readdirSync(testFilesPath).find(fileName => fileName.endsWith('.json') && fileName.startsWith(recordType));
    const parsedData: InputData = JSON.parse(fs.readFileSync(testFilesPath + jsonFile, 'utf-8'));
    const inputParticipantRecord: Record<string, any> = parsedData.inputParticipantRecord;
    const nhsNumbers: string[] = parsedData.nhsNumbers;
    return [parsedData.validations, inputParticipantRecord, nhsNumbers, testFilesPath];
  });
}

export async function getApiQueryParams(scenarioFolderName: string, recordType: string): Promise<any> {
  return test.step(`Creating Input Data from JSON file`, async () => {
    console.info('🏃‍♂️‍➡️\tRunning test For: ', scenarioFolderName);
    const testFilesPath = path.join(__dirname, `../`, `${config.apiTestFilesPath}/${scenarioFolderName.substring(0, 14)}/`);
    const jsonFile = fs.readdirSync(testFilesPath).find(fileName => fileName.endsWith('.json') && fileName.startsWith(recordType));
    const parsedData: InputData = JSON.parse(fs.readFileSync(testFilesPath + jsonFile, 'utf-8'));
    return parsedData.queryParams;
  });
}

export async function verifyBlobExists(stepName: string, filePath: string) {
  await test.step(stepName, async () => {
    const expectedBlobName = path.basename(filePath);
    const outputFileExists = await checkBlobExists(expectedBlobName);
    expect(outputFileExists).toBe(true);
  });
}

export async function getLatestValidDatefromDatabase(
  request: APIRequestContext,
  getDataFromDatabse: (request: APIRequestContext) => Promise<any[]>,
  dateField: string,
  description = `Get latest valid database record date`
): Promise<string> {
  return await test.step(description, async () => {
    const result = await getDataFromDatabse(request);

    const validRecords = result.filter((r: Record<string, any>) =>
      r[dateField] && r[dateField].trim() !== '' && r[dateField] !== '0001-01-01T00:00:00'
    );

    if (validRecords.length === 0) {
      throw new Error(`No valid records found for expected date: "${dateField}"`);
    }

    validRecords.sort((a, b) =>
      new Date(b[dateField]).getTime() - new Date(a[dateField]).getTime()
    );

    const currentDate = validRecords[0][dateField].split('T')[0] + "T00:00:00";
    console.log(`Latest ${dateField}: ${currentDate}`);

    return currentDate;
  });
}
