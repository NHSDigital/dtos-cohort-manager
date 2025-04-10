import { test, expect, APIRequestContext } from "@playwright/test";
import { validateSqlData } from "../../database/sqlVerifier";
import { uploadToLocalStorage } from "../../storage/azureStorage";
import { InputData } from "../../interface/InputData";
import { config } from "../../config/env";
import * as fs from 'fs';
import path from "path";
<<<<<<< HEAD
import { validateApiResponse } from "../../api/apiHelper";
=======
>>>>>>> main

export async function validateSqlDatabase(validations: any) {
  return test.step(`Validate database for assertions`, async () => {
    const hasFailures = await validateSqlData(validations);

    try {
      expect(hasFailures).toBeTruthy();
    } catch (error) {
      console.error("Test has failures, please check logs for errors");
      throw error;
    }
  });
}

<<<<<<< HEAD

export async function validateSqlDatabaseFromAPI(request: APIRequestContext, validations: any) {
  return test.step(`Validate database for assertions`, async () => {
    const hasFailures = await validateApiResponse(validations, request);
    try {
      expect(hasFailures).toBeFalsy();
    } catch (error) {
      console.error("Test has failures, please check logs for errors");
      throw error;
    }
  });
}


export async function processFileViaStorage(parquetFilePath: string) {
  return test.step(`Process file via Storage`, async () => {
    await uploadToLocalStorage(parquetFilePath);
  });
}

export async function getTestData(scenarioFolderName: string, recordType: string = "ADD"): Promise<[any, string[], string?]> { //TODO fix return type
  return test.step(`Creating Input Data from JSON file`, async () => {
    const testScenariosPath = path.join(__dirname, `../`, `${config.e2eTestScenarioPath}/${scenarioFolderName.substring(0, 2)}/`);
    console.info(`ℹ️\tTest scenarios input data path: ${testScenariosPath}`);
    const jsonFile = fs.readdirSync(testScenariosPath).find(fileName => fileName.endsWith('.json') && fileName.startsWith(recordType));
    const parquetFile = testScenariosPath + jsonFile?.replace('.json', '.parquet'); //TODO add a check here to fail if jsonFile name is not same as parquet file name
    const parsedData: InputData = JSON.parse(fs.readFileSync(testScenariosPath + jsonFile, 'utf-8'));
    const nhsNumbers: string[] = parsedData.validations.map(item => item.validations.nhsNumber);
=======
export async function processFileViaStorage(parquetFilePath: string) {
  return test.step(`Process file via Storage`, async () => {
    await uploadToLocalStorage(parquetFilePath);
  });
}

export async function getTestData(scenarioFolderName: string, recordType: string = "ADD"): Promise<[any, string[], string?]> { //TODO fix return type
  return test.step(`Creating Input Data from JSON file`, async () => {
    const testFilesPath = path.join(__dirname, `../`, `${config.e2eTestFilesPath}/${scenarioFolderName.substring(0, 2)}/`);
    console.info(`ℹ️\tTest files input data path: ${testFilesPath}`);
    const jsonFile = fs.readdirSync(testFilesPath).find(fileName => fileName.endsWith('.json') && fileName.startsWith(recordType));
    const parquetFile = testFilesPath + jsonFile?.replace('.json', '.parquet'); //TODO add a check here to fail if jsonFile name is not same as parquet file name
    const parsedData: InputData = JSON.parse(fs.readFileSync(testFilesPath + jsonFile, 'utf-8'));
    const nhsNumbers: string[] = parsedData.validations.map(item => item.validations.columnValue);
>>>>>>> main
    // TODO integrate Parquet file creation process here
    return [parsedData.validations, nhsNumbers, parquetFile];
  });
}
