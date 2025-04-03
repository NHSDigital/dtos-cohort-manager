import { test, expect } from "@playwright/test";
import { validateSqlData } from "../../database/sqlVerifier";
import path from "path";
import { uploadToLocalStorage } from "../../storage/azureStorage";

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
      const parquetFilePath = path.join(__dirname, `../`,`e2e/testFiles/${fileName}`); // TODO move static data to configuration file .env.*
      await uploadToLocalStorage(parquetFilePath);
  });
}

