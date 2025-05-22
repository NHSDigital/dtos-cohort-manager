import { test, testWithAmended, expect } from '../../fixtures/test-fixtures';
import { cleanupDatabaseFromAPI, processFileViaStorage, validateSqlDatabaseFromAPI } from '../../steps/steps';
import { TestHooks } from '../../hooks/test-hooks';

test.describe('@regression @e2e @epic2-high-priority Tests', () => {

  TestHooks.setupAllTestHooks();

  test.describe('ADD Tests', () => {

    test('@DTOSS-5104-01 @Implement Validation for Eligibility Flag for Add', async ({ request, testData }) => {
      await test.step(`Then NHS Numbers should be updated in the cohort`, async () => {
        await validateSqlDatabaseFromAPI(request, testData.checkInDatabase);
      });

      await test.step(`Then validate eligibility flag is always set to true for ADD`, async () => {
        await validateSqlDatabaseFromAPI(request, testData.checkInDatabase);
      });
    });

    test('@DTOSS-5613-01 @Implement Validation participant for Eligibility Flag set to true for Add', async ({ request, testData }) => {
      await test.step(`Then only one NHS Numbers should be updated in the cohort for elgibility flag true`, async () => {
        await validateSqlDatabaseFromAPI(request, testData.checkInDatabase);
      });
    });

    test('@DTOSS-4395-01 @Implement validate invalid flag set to false for ADD', async ({ request, testData }) => {
      await test.step(`Then validate invalid flag is set to false for ADD`, async () => {
        await validateSqlDatabaseFromAPI(request, testData.checkInDatabase);
      });
    });

    test('@DTOSS-4397-01 @Implement validate invalid flag set to true for ADD', async ({ request, testData }) => {
      await test.step(`Then validate invalid flag is set to true for ADD`, async () => {
        await validateSqlDatabaseFromAPI(request, testData.checkInDatabase);
      });
    });

    test('@DTOSS-4562-01 @Implement Validation for Eligibility Flag for Add set to true', async ({ request, testData }) => {
      await test.step(`Then validate eligibility flag will always revert to true for ADD`, async () => {
        await validateSqlDatabaseFromAPI(request, testData.checkInDatabase);
      });

      await test.step(`Then ADD record should be updated in the cohort`, async () => {
        await validateSqlDatabaseFromAPI(request, testData.checkInDatabase);
      });
    });

    test('@DTOSS-4563-01 @Implement Validation for Eligibility Flag for Add set to false ends up in Exception', async ({ request, testData }) => {
      await test.step(`Then there should be exception in exception management table`, async () => {
        await validateSqlDatabaseFromAPI(request, testData.checkInDatabase);
      });
    });

    test('@DTOSS-3206-01 @Duplicate NHS ID Participants records', async ({ request, testData }) => {
      await test.step(`Then NHS Numbers should be updated in the participant table`, async () => {
        await validateSqlDatabaseFromAPI(request, testData.checkInDatabase);
      });

      await test.step(`When ADD participant is processed via storage again`, async () => {
        await processFileViaStorage(testData.runTimeParquetFile);
      });

      await test.step(`Then NHS Numbers should be updated in the participant table after second run`, async () => {
        await validateSqlDatabaseFromAPI(request, testData.checkInDatabase);
      });
    })

    test('@DTOSS-4136-01 @Validate NHS number 10 digits', async ({ request, testData }) => {
      await test.step(`Then NHS Numbers should be updated in the participant table`, async () => {
        await validateSqlDatabaseFromAPI(request, testData.checkInDatabase);
      });
    })

    test('@DTOSS-4139-01 @Validate NHS number 11 digits', async ({ request, testData }) => {
      await test.step(`Then NHS Numbers should be updated in the Exception table`, async () => {
        await validateSqlDatabaseFromAPI(request, testData.checkInDatabase);
      });
    })

    test('@DTOSS-4140-01 @Validate NHS number 9 digits', async ({ request, testData }) => {
      await test.step(`Then NHS Numbers should be updated in the Exception table`, async () => {
        await validateSqlDatabaseFromAPI(request, testData.checkInDatabase);
      });
    })

    test('@DTOSS-4141-01 @Validate NHS number as null', async ({ request, testData }) => {
      await test.step(`Then NHS Numbers should be updated in the Exception table`, async () => {
        await validateSqlDatabaseFromAPI(request, testData.checkInDatabase);
      });
    })

    test('@DTOSS-4321-01 Validate Rule_Id as -2146233088 & Rule_Description as Invalid effective date found in participant data Model.Participant and file name ADD6_-_CAAS_BREAST_SCREENING_COHORT.parquet', {
      annotation: {
        type: 'Requirement',
        description: 'Tests - https://nhsd-jira.digital.nhs.uk/browse/DTOSS-3136',
      },
    }, async ({ request, testData }) => {
      await test.step(`Then Exception table should have expected rule id and description for 6 ADD participants`, async () => {
        await validateSqlDatabaseFromAPI(request, testData.checkInDatabase);
      });
    })

  }); // End of ADD Tests

  test.describe('AMENDED Tests', () => {

    testWithAmended('@DTOSS-5605-01 @Implement Validation for Eligibility Flag for Amended', async ({ request, testData }) => {
      await test.step(`Then ADD record should be updated in the cohort`, async () => {
        await validateSqlDatabaseFromAPI(request, testData.checkInDatabaseAdd);
      });

      await test.step(`When same ADD participant record is AMENDED via storage for ${testData.nhsNumberAmend}`, async () => {
        await processFileViaStorage(testData.runTimeParquetFileAmend);
      });

      await test.step(`Then validate eligibility flag is always set to true for AMENDED`, async () => {
        await validateSqlDatabaseFromAPI(request, testData.checkInDatabaseAmend);
      });
    });

    testWithAmended('@DTOSS-4396-01 @Implement validate invalid flag value for Amended', async ({ request, testData }) => {
      await test.step(`Then validate invalid flag is set to true for ADD`, async () => {
        await validateSqlDatabaseFromAPI(request, testData.checkInDatabaseAdd);
      });

      await test.step(`When same ADD participant record is AMENDED via storage for ${testData.nhsNumberAmend}`, async () => {
        await processFileViaStorage(testData.runTimeParquetFileAmend);
      });

      await test.step(`Then validate invalid flag set to true for AMENDED`, async () => {
        await validateSqlDatabaseFromAPI(request, testData.checkInDatabaseAmend);
      });
    });

    testWithAmended('@DTOSS-5419-01 @validate Gp practice codes and reason for removal no exception for Amended', async ({ request, testData }) => {
      await test.step(`Then ADD record should be updated in the cohort`, async () => {
        await validateSqlDatabaseFromAPI(request, testData.checkInDatabaseAdd);
      });

      await test.step(`When same ADD participant record is AMENDED via storage`, async () => {
        await processFileViaStorage(testData.runTimeParquetFileAmend);
      });

      await test.step(`Then AMENDED record should be updated in the cohort`, async () => {
        await validateSqlDatabaseFromAPI(request, testData.checkInDatabaseAmend);
      });
    });

    testWithAmended('@DTOSS-4068-01 @Implement Validate Amend fields date of death', async ({ request, testData }) => {
      await test.step(`Then validate nhs number for ADD in participant demographic`, async () => {
        await validateSqlDatabaseFromAPI(request, testData.checkInDatabaseAdd);
      });

      await test.step(`When same ADD participant record is AMENDED via storage for ${testData.nhsNumberAmend}`, async () => {
        await processFileViaStorage(testData.runTimeParquetFileAmend);
      });

      await test.step(`Then validate the values changed in AMENDED in Participant demographic`, async () => {
        await validateSqlDatabaseFromAPI(request, testData.checkInDatabaseAmend);
      });
    });

    testWithAmended('@DTOSS-4070-01 @Validate Amend fields Reason for Removal,Reason for Removal Business Effective From Date,E-mail address (Home)', async ({ request, testData }) => {
      await test.step(`Then validate nhs number for ADD in participant demographic`, async () => {
        await validateSqlDatabaseFromAPI(request, testData.checkInDatabaseAdd);
      });

      await test.step(`When same ADD participant record is AMENDED via storage for ${testData.nhsNumberAmend}`, async () => {
        await processFileViaStorage(testData.runTimeParquetFileAmend);
      });

      await test.step(`Then validate the values changed in AMENDED in Participant demographic`, async () => {
        await validateSqlDatabaseFromAPI(request, testData.checkInDatabaseAmend);
      });
    });

    testWithAmended('@DTOSS-4564-01 @Validate date of birth,Address,Address line 1,Address line 2,Address line 3,post code,SupersededNHSNumber,RecordInsertDateTime,RecordUpdateDateTime,E-mail address (Home)', async ({ request, testData }) => {

      await test.step(`Then validate nhs number for ADD in participant demographic`, async () => {
        await validateSqlDatabaseFromAPI(request, testData.checkInDatabaseAdd);
      });

      await test.step(`When same ADD participant record is AMENDED via storage for ${testData.nhsNumberAmend}`, async () => {
        await processFileViaStorage(testData.runTimeParquetFileAmend);
      });

      await test.step(`Then validate the values changed in AMENDED in Participant demographic`, async () => {
        await validateSqlDatabaseFromAPI(request, testData.checkInDatabaseAmend);
      });
    });
  });

});
