import { test as base } from '@playwright/test';
import { getTestData } from '../steps/steps';
import { createParquetFromJson } from '../../parquet/parquet-multiplier';
import path from 'path';
import fs from 'fs';


interface TestData {
  checkInDatabase: any[];
  nhsNumbers: string[];
  runTimeParquetFile: string;
  runTimeParquetFileInvalid: string;
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

interface TestDataWithAmended2 {
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

  checkInDatabaseAmend2: any[];
  nhsNumberAmend2: string[];
  parquetFileAmend2: string | undefined;
  inputParticipantRecordAmend2?: Record<string, any>;
  testFilesPathAmend2?: string;
  runTimeParquetFileAmend2: string;
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

    const tempFileName = "Exception_1B8F53_-_CAAS_BREAST_screening_@.parquet";
    const tempDir = path.join(__dirname, 'temp');
    if (!fs.existsSync(tempDir)) {
      fs.mkdirSync(tempDir);
    }
    const runTimeParquetFileInvalid = path.join(tempDir, tempFileName);
    fs.writeFileSync(runTimeParquetFileInvalid, 'Dummy content for invalid file parquet file name validation.');

    const testData: TestData = {
      checkInDatabase,
      nhsNumbers,
      runTimeParquetFile,
      runTimeParquetFileInvalid,
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

export const testWithAmended2 = base.extend<{
  testData: TestDataWithAmended2;
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

    const [checkInDatabaseAmend2, nhsNumberAmend2, parquetFileAmend2, inputParticipantRecordAmend2, testFilesPathAmend2] =
    await getTestData(testInfo.title, "AMENDED2", true);

  let runTimeParquetFileAmend2: string = "";
  if (!parquetFileAmend) {
    runTimeParquetFileAmend2 = await createParquetFromJson(
      nhsNumberAmend2,
      inputParticipantRecordAmend2!,
      testFilesPathAmend2!,
      "AMENDED2",
      false
    );
  }

    const testDataWithAmended2: TestDataWithAmended2 = {
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
      runTimeParquetFileAmend,

      checkInDatabaseAmend2,
      nhsNumberAmend2,
      parquetFileAmend2,
      inputParticipantRecordAmend2,
      testFilesPathAmend2,
      runTimeParquetFileAmend2
    };

    await use(testDataWithAmended2);
  },
});

export { expect } from '@playwright/test';
