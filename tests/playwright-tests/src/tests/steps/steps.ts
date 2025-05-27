import { test, APIRequestContext, expect } from "@playwright/test";
import { checkBlobExists, uploadToLocalStorage } from "../../storage/azureStorage";
import { InputData } from "../../interface/InputData";
import { config } from "../../config/env";
import * as fs from 'fs';
import path from "path";
import { validateApiResponse } from "../../api/apiHelper";
import { cleanDataBaseUsingServices } from "../../api/dataService/dataServiceCleaner";
import { ensureNhsNumbersStartWith999 } from "../fixtures/testDataHelper";


export async function cleanupDatabaseFromAPI(request: APIRequestContext, numbers: string[]) {
  return test.step(`Cleanup database using data services`, async () => {
    await cleanDataBaseUsingServices(numbers, request);
  });
}

export async function validateSqlDatabaseFromAPI(request: APIRequestContext, validations: any) {
  return test.step(`Validate database for assertions`, async () => {
    const status = await validateApiResponse(validations, request);
    if (!status) {
      throw new Error(`❌ Validation failed after ${config.apiRetry} attempts, please checks logs for more details`);
    }
  });
}

export async function processFileViaStorage(parquetFilePath: string) {
  return test.step(`Process file via Storage`, async () => {
    await uploadToLocalStorage(parquetFilePath);
  });
}

export async function getTestData(scenarioFolderName: string
  , recordType: string = "ADD"
  , createParquetFile = false):  Promise<[any, string[], string?, Record<string, any>?, string?]> { //TODO fix return type
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

    const rawNhsNumbers: string[] = parsedData.validations.map(item =>
      String(item.validations.NHSNumber || item.validations.NhsNumber)
    );

    const nhsNumbers = ensureNhsNumbersStartWith999(rawNhsNumbers);

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

export async function verifyBlobExists(stepName: string, filePath: string) {
  await test.step(stepName, async () => {
    const expectedBlobName = path.basename(filePath);
    const outputFileExists = await checkBlobExists(expectedBlobName);
    expect(outputFileExists).toBe(true);
  });
}

export async function validateRecordNotInDatabase(request: APIRequestContext, validations: any) {
  return test.step(`Validate record is NOT present in the database`, async () => {
    const status = await validateApiResponse(validations, request);
    if (status) {
      throw new Error(`❌ Record should have been rejected but was found in the database.`);
    }
  });
}
