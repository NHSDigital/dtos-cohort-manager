import { test, testWithAmended } from '../../fixtures/test-fixtures';
import { processFileViaStorage, validateSqlDatabaseFromAPI, verifyBlobExists } from '../../steps/steps';
import { TestHooks } from '../../hooks/test-hooks';
import { createParquetFromJson } from '../../../parquet/parquet-multiplier';
import { createTempDirAndWriteJson, deleteTempDir } from '../../../../src/json/file-utils';
import { generateDynamicDateMap, replaceDynamicDatesInJson } from '../../../../src/json/json-updater';

test.describe('@regression @e2e @epic2-high-priority Tests', () => {

  TestHooks.setupAllTestHooks();

  test.describe('ADD Tests for NHS Number format', () => {

    test('@DTOSS-4139-01 @not-runner-based @Validate NHS number 11 digits', async ({ request, testData }) => {
      await test.step(`Then NHS Numbers should be updated in the Exception table`, async () => {
        await validateSqlDatabaseFromAPI(request, testData.checkInDatabase);
      });
    })

    test('@DTOSS-4140-01 @not-runner-based @Validate NHS number 9 digits', async ({ request, testData }) => {
      await test.step(`Then NHS Numbers should be updated in the Exception table`, async () => {
        await validateSqlDatabaseFromAPI(request, testData.checkInDatabase);
      });
    })

    test('@DTOSS-4141-01 @not-runner-based @Validate NHS number as null', async ({ request, testData }) => {
      await test.step(`Then NHS Numbers should be updated in the Exception table`, async () => {
        await validateSqlDatabaseFromAPI(request, testData.checkInDatabase);
      });
    })
  });
  // End of ADD Tests

  testWithAmended('@DTOSS-4331-01 Validate current posting effective date throw exception for future date amend participants', {
    annotation: {
      type: 'Requirement',
      description: 'Tests - https://nhsd-jira.digital.nhs.uk/browse/DTOSS-3136',
    },
  }, async ({ request, testData }) => {
    await test.step(`Given 3 ADD participants are processed to cohort`, async () => {
      await validateSqlDatabaseFromAPI(request, testData.checkInDatabaseAdd);
    });

    await test.step(`And 3 ADD participants are AMENDED with future effective date `, async () => {

      const updatedParticipantRecord = JSON.parse(JSON.stringify(testData.inputParticipantRecordAmend))

      const dateMap = generateDynamicDateMap();

      const finalJson = replaceDynamicDatesInJson(updatedParticipantRecord, dateMap);

      const tempFilePath = createTempDirAndWriteJson(finalJson);

      const runTimeParquetFile = await createParquetFromJson(testData.nhsNumberAmend, finalJson, tempFilePath, "AMENDED", false);
      await processFileViaStorage(runTimeParquetFile);
      deleteTempDir();

    });
    await test.step(`Then Exception table should have expected rule id and description for 3 AMENDED participants`, async () => {
      await validateSqlDatabaseFromAPI(request, testData.checkInDatabaseAmend);
    });
  });

});
