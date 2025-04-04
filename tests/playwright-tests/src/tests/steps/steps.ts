import { test, expect } from "@playwright/test";
import { validateSqlData } from "../../database/sqlVerifier";
import { uploadToLocalStorage } from "../../storage/azureStorage";
import { InputData } from "../../interface/InputData";
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

export async function processFileViaStorage(fileName: string) {
  return test.step(`Process file via Storage`, async () => {
    const parquetFilePath = path.join(__dirname, `../`, `e2e/testFiles/${fileName}`); // TODO move static data to configuration file .env.*
    await uploadToLocalStorage(parquetFilePath);
  });
}

export async function getTestData(fileName: string): Promise<[any, string[]]> { //TODO fix return type
  return test.step(`Extracting data to be validated from input JSON`, async () => {
    const parsedData: InputData = JSON.parse(fs.readFileSync(path.join(__dirname, `../`, `e2e/testFiles/${fileName}`), 'utf-8')); // TODO move static data to configuration file .env
    const nhsNumbers: string[] = parsedData.validations.map(item => item.validations.columnValue);
    return [parsedData.validations, nhsNumbers];
  });
}
