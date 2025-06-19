import { test } from '../../fixtures/test-fixtures';
import { cleanupDatabaseFromAPI, processFileViaStorage, validateSqlDatabaseFromAPI } from '../../steps/steps';


test.describe('@regression @e2e @epic1-high-priority @not-runner-based invalid file for ADD process', () => {
  test('@DTOSS-3192-01 Verify that a file with an invalid name creates a validation exception', async ({ request, testData }) => {
    await test.step(`Given database does not contain 1 ADD record that will be processed`, async () => {
      await cleanupDatabaseFromAPI(request, testData.nhsNumbers);
    });

    await processFileViaStorage(testData.runTimeParquetFileInvalid);


    await test.step(`Then exception should have 1 record with The file failed file validation. Check the file Exceptions blob store. and rule id 0`, async () => {
      await validateSqlDatabaseFromAPI(request, testData.checkInDatabase);
    });


  });
});










