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

interface TestDataWithTwoAmendments {
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

  checkInDatabaseSecondAmend: any[];
  nhsNumberSecondAmend: string[];
  parquetFileSecondAmend: string | undefined;
  inputParticipantRecordSecondAmend?: Record<string, any>;
  testFilesPathSecondAmend?: string;
  runTimeParquetFileSecondAmend: string;
}

export const test = base.extend<{
  testData: TestData;
}>({
  testData: async ({ request: _ }, use, testInfo) => {
    const [checkInDatabase, nhsNumbers, parquetFile, inputParticipantRecord, testFilesPath] =
      await getTestData(testInfo.title, "ADD", true);

    let runTimeParquetFile: string = "";
    // Skip parquet creation if nhsNumbers contain '', undefined, or are empty
    const hasInvalidNhsNumbers = nhsNumbers.some(
      n => n === '' || n === undefined
    );
    if (!parquetFile && !hasInvalidNhsNumbers) {
      const multiplyRecords =
      nhsNumbers.length !== (Array.isArray(inputParticipantRecord) ? inputParticipantRecord.length : nhsNumbers.length);
      runTimeParquetFile = await createParquetFromJson(
      nhsNumbers,
      inputParticipantRecord!,
      testFilesPath!,
      "ADD",
      multiplyRecords
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

export const testWithTwoAmendments = base.extend<{
  testData: TestDataWithTwoAmendments;
}>({
  testData: async ({ request }, use, testInfo) => {
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

    const [checkInDatabaseSecondAmend, nhsNumberSecondAmend, parquetFileSecondAmend, inputParticipantRecordSecondAmend, testFilesPathSecondAmend] =
      await getTestData(testInfo.title, "AMENDED2", true);

    let runTimeParquetFileSecondAmend: string = "";
    if (!parquetFileSecondAmend) {
      runTimeParquetFileSecondAmend = await createParquetFromJson(
        nhsNumberSecondAmend,
        inputParticipantRecordSecondAmend!,
        testFilesPathSecondAmend!,
        "AMENDED2",
        false
      );
    }

    const testDataWithTwoAmendments: TestDataWithTwoAmendments = {
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

      checkInDatabaseSecondAmend,
      nhsNumberSecondAmend,
      parquetFileSecondAmend,
      inputParticipantRecordSecondAmend,
      testFilesPathSecondAmend,
      runTimeParquetFileSecondAmend
    };

    await use(testDataWithTwoAmendments);
  },
});


export { expect } from '@playwright/test';
