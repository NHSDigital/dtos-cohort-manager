import { test, APIRequestContext } from "@playwright/test";
import { uploadToLocalStorage } from "../../storage/azureStorage";
import { InputData } from "../../interface/InputData";
import { config } from "../../config/env";
import * as fs from 'fs';
import path from "path";
import { validateApiResponse } from "../../api/apiHelper";
import { cleanDataBaseUsingServices } from "../../api/dataService/dataServiceCleaner";


export async function cleanupDatabaseFromAPI(request: APIRequestContext, numbers: string[]) {
  return test.step(`Cleanup database using data services`, async () => {
    await cleanDataBaseUsingServices(numbers, request);
  });
}

export async function validateSqlDatabaseFromAPI(request: APIRequestContext, validations: any) {
  return test.step(`Validate database for assertions`, async () => {
    const status = await validateApiResponse(validations, request);
    if(!status){
      throw new Error(`❌ Validation failed after ${config.apiRetry} attempts, please checks logs for more details`);
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
    const testFilesPath = path.join(__dirname, `../`, `${config.e2eTestFilesPath}/${scenarioFolderName.substring(0, 2)}/`);
    console.info(`ℹ️\tTest files input data path: ${testFilesPath}`);
    const jsonFile = fs.readdirSync(testFilesPath).find(fileName => fileName.endsWith('.json') && fileName.startsWith(recordType));
    const parquetFile = testFilesPath + jsonFile?.replace('.json', '.parquet'); //TODO add a check here to fail if jsonFile name is not same as parquet file name
    const parsedData: InputData = JSON.parse(fs.readFileSync(testFilesPath + jsonFile, 'utf-8'));
    const nhsNumbers: string[] = parsedData.validations.map(item =>
      String(item.validations.NHSNumber || item.validations.NhsNumber)
    );
    // TODO integrate Parquet file creation process here during test execution
    return [parsedData.validations, nhsNumbers, parquetFile];
  });
}
