import { test, expect } from "@playwright/test";
import { validateSqlData } from "../../database/sqlVerifier";
import { uploadToLocalStorage } from "../../storage/azureStorage";
import { InputData } from "../../interface/InputData";
import { config } from "../../config/env";
import * as fs from 'fs';
import path from "path";

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

export async function processFileViaStorage(parquetFilePath: string) {
  return test.step(`Process file via Storage`, async () => {
    await uploadToLocalStorage(parquetFilePath);
  });
}

export async function getTestData(scenarioFolderName: string, recordType: string = "ADD"): Promise<[any, string[], string?]> { //TODO fix return type
  return test.step(`Creating Input Data from JSON file`, async () => {
    const testScenariosPath = path.join(__dirname, `../`, `${config.e2eTestScenarioPath}/${scenarioFolderName.substring(0, 2)}/`);
    console.info(`ℹ️ Test scenarios input data path: ${testScenariosPath}`);
    const jsonFile = fs.readdirSync(testScenariosPath).find(fileName => fileName.endsWith('.json') && fileName.startsWith(recordType));
    const parquetFile = testScenariosPath + jsonFile?.replace('.json', '.parquet'); //TODO add a check here to fail if jsonFile name is not same as parquet file name
    const parsedData: InputData = JSON.parse(fs.readFileSync(testScenariosPath + jsonFile, 'utf-8'));
    const nhsNumbers: string[] = parsedData.validations.map(item => item.validations.columnValue);
    // TODO integrate Parquet file creation process here
    return [parsedData.validations, nhsNumbers, parquetFile];
  });
}
