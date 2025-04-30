import { test as base } from '@playwright/test';
import { getTestData } from '../steps/steps';
import { createParquetFromJson } from '../../parquet/parquet-multiplier';


interface TestData {
  checkInDatabase: any[];
  nhsNumbers: string[];
  runTimeParquetFile: string;
  inputParticipantRecord?: Record<string, any>;
  testFilesPath?: string;
}


interface TestDataWithAmended {
  checkInDatabaseAdd: any[];
  nhsNumbers: string[];
  parquetFile: string | undefined;
  inputParticipantRecord?: Record<string, any>;
  testFilesPath?: string;
  runTimeParquetFileAdd: string;

  checkInDatabaseAmend: any[];
  nhsNumberAmend: string[];
  parquetFileAmend: string | undefined;
  inputParticipantRecordAmend?: Record<string, any>;
  testFilesPathAmend?: string;
  runTimeParquetFileAmend: string;
}

export const test = base.extend<{
  testData: TestData;
}>({
  testData: async ({ request: _ }, use, testInfo) => {
    const [checkInDatabase, nhsNumbers, parquetFile, inputParticipantRecord, testFilesPath] =
      await getTestData(testInfo.title, "ADD", true);

    let runTimeParquetFile: string = "";
    if (!parquetFile) {
      runTimeParquetFile = await createParquetFromJson(
        nhsNumbers,
        inputParticipantRecord!,
        testFilesPath!,
        "ADD",
        false
      );
    }

    const testData: TestData = {
      checkInDatabase,
      nhsNumbers,
      runTimeParquetFile,
      inputParticipantRecord,
      testFilesPath
    };

    await use(testData);
  },
});


export const testWithAmended = base.extend<{
  testData: TestDataWithAmended;
}>({
  testData: async ({ request: _ }, use, testInfo) => {
    const [checkInDatabaseAdd, nhsNumbers, parquetFile, inputParticipantRecord, testFilesPath] =
      await getTestData(testInfo.title, "ADD", true);

    let runTimeParquetFileAdd: string = "";
    if (!parquetFile) {
      runTimeParquetFileAdd = await createParquetFromJson(
        nhsNumbers,
        inputParticipantRecord!,
        testFilesPath!,
        "ADD",
        false
      );
    }

    const [checkInDatabaseAmend, nhsNumberAmend, parquetFileAmend, inputParticipantRecordAmend, testFilesPathAmend] =
      await getTestData(testInfo.title, "AMENDED", true);

    let runTimeParquetFileAmend: string = "";
    if (!parquetFileAmend) {
      runTimeParquetFileAmend = await createParquetFromJson(
        nhsNumberAmend,
        inputParticipantRecordAmend!,
        testFilesPathAmend!,
        "AMENDED",
        false
      );
    }

    const testDataWithAmended: TestDataWithAmended = {
      checkInDatabaseAdd,
      nhsNumbers,
      parquetFile,
      inputParticipantRecord,
      testFilesPath,
      runTimeParquetFileAdd,

      checkInDatabaseAmend,
      nhsNumberAmend,
      parquetFileAmend,
      inputParticipantRecordAmend,
      testFilesPathAmend,
      runTimeParquetFileAmend
    };

    await use(testDataWithAmended);
  },
});


export { expect } from '@playwright/test';
