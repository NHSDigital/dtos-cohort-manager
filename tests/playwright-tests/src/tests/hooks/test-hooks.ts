// hooks/test-hooks.ts
import { test, testWithAmended } from '../fixtures/test-fixtures';
import { cleanupDatabaseFromAPI, processFileViaStorage } from '../steps/steps';


export class TestHooks {

  static setupAddTestHooks(testBlock = test): void {
    testBlock.beforeEach(async ({ request, testData }) => {
      await test.step(`Given database does not contain record that will be processed`, async () => {
        await cleanupDatabaseFromAPI(request, testData.nhsNumbers);
      });

      await test.step(`When ADD participant is processed via storage`, async () => {
        if (testData.runTimeParquetFile) {
          await processFileViaStorage(testData.runTimeParquetFile);
        } else {
          console.warn('runTimeParquetFile is undefined, skipping file upload');
        }
      });
    });
  }


  static setupAmendedTestHooks(testBlock = testWithAmended): void {
    testBlock.beforeEach(async ({ request, testData }) => {
      await test.step(`Given database does not contain record that will be processed`, async () => {
        if (testData.nhsNumbers) {
          await cleanupDatabaseFromAPI(request, testData.nhsNumbers);
        }
        if (testData.nhsNumberAmend) {
          await cleanupDatabaseFromAPI(request, testData.nhsNumberAmend);
        }
      });

      await test.step(`When ADD participant is processed via storage`, async () => {
        if (testData.runTimeParquetFileAdd) {
          await processFileViaStorage(testData.runTimeParquetFileAdd);
        } else {
          console.warn('runTimeParquetFileAdd is undefined, skipping file upload');
        }
      });
    });
  }


  static setupAllTestHooks(): void {
    this.setupAddTestHooks();
    this.setupAmendedTestHooks();
  }
}
