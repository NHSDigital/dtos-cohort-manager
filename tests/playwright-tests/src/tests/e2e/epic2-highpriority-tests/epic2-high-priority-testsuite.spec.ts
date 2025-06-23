import { test, testWithAmended} from '../../fixtures/test-fixtures';
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

    test('@DTOSS-4328-01 Validate current posting effective date throw exception when invalid date format given for new participants', {
      annotation: {
        type: 'Requirement',
        description: 'Tests - https://nhsd-jira.digital.nhs.uk/browse/DTOSS-3136',
      },
    }, async ({ request, testData }) => {
      await test.step(`Then Exception table should have RuleId as 101 & RuleDescription as CurrentPostingEffectiveFromDate`, async () => {
        await validateSqlDatabaseFromAPI(request, testData.checkInDatabase);
      });
    })

    test('@DTOSS-4102-01-Validate valid GP Practice Code for a new participant', {
      annotation: {
        type: 'Requirement',
        description: 'Tests - https://nhsd-jira.digital.nhs.uk/browse/DTOSS-4102',
      },
    }, async ({ request, testData }) => {

      await test.step(`Then the record should appear in the cohort management table`, async () => {
        await validateSqlDatabaseFromAPI(request, testData.checkInDatabase);
      });
    })

  test('@DTOSS-4088-01-Validate valid postcode', {
      annotation: {
        type: 'Requirement',
        description: 'Tests - https://nhsd-jira.digital.nhs.uk/browse/DTOSS-4088',
      },
    }, async ({ request, testData }) => {

      await test.step(`Then the record should appear in the exception table`, async () => {
        await validateSqlDatabaseFromAPI(request, testData.checkInDatabase);
      });
  })

    test('@DTOSS-4103-01-Validate invalid GP Practice Code for a new participant', {
      annotation: {
        type: 'Requirement',
        description: 'Tests - https://nhsd-jira.digital.nhs.uk/browse/DTOSS-4103',
      },
    }, async ({ request, testData }) => {

    await test.step(`Then the record should appear in the exception table`, async () => {
      await validateSqlDatabaseFromAPI(request, testData.checkInDatabase);
    });
  })

  test('@DTOSS-4099-01-Validate missing address lines', {
      annotation: {
        type: 'Requirement',
        description: 'Tests - https://nhsd-jira.digital.nhs.uk/browse/DTOSS-4099',
      },
    }, async ({ request, testData }) => {

      await test.step(`Then the record should appear in the exception table`, async () => {
        await validateSqlDatabaseFromAPI(request, testData.checkInDatabase);
      });
  })

  test('@DTOSS-4092-01-Validate null GP Practice Code for a new participant', {
      annotation: {
        type: 'Requirement',
        description: 'Tests - https://nhsd-jira.digital.nhs.uk/browse/DTOSS-4092',
      },
    }, async ({ request, testData }) => {

      await test.step(`Then the record should appear in the exception table`, async () => {
        await validateSqlDatabaseFromAPI(request, testData.checkInDatabase);
      });
  })
  // End of ADD Tests

  testWithAmended('@DTOSS-4384-01-Update a invalid GP Practice Code for a existing participant', {
      annotation: {
        type: 'Requirement',
        description: 'Tests - https://nhsd-jira.digital.nhs.uk/browse/DTOSS-4384',
      },
    }, async ({ request, testData }) => {

      await test.step(`When ADD participant is processed via storage`, async () => {
        await processFileViaStorage(testData.runTimeParquetFileAdd);
      });

      await verifyBlobExists('Verify ProcessCaasFile data file', testData.runTimeParquetFileAdd);

      await test.step(`Given 1 participant is processed to cohort`, async () => {
        await validateSqlDatabaseFromAPI(request, testData.checkInDatabaseAdd);
      });

      await test.step(`When same ADD participant record is AMENDED with an invalid GP code via storage for ${testData.nhsNumberAmend}`, async () => {
        await processFileViaStorage(testData.runTimeParquetFileAmend);
      });

      await test.step(`Then the record should not be amended in the cohort`, async () => {
        await validateSqlDatabaseFromAPI(request, testData.checkInDatabaseAmend);
      });
  })

  testWithAmended('@DTOSS-4383-01-Update a valid GP Practice Code for a existing participant', {
      annotation: {
        type: 'Requirement',
        description: 'Tests - https://nhsd-jira.digital.nhs.uk/browse/DTOSS-4383',
      },
    }, async ({ request, testData }) => {

      await test.step(`When ADD participant is processed via storage`, async () => {
        await processFileViaStorage(testData.runTimeParquetFileAdd);
      });

      await verifyBlobExists('Verify ProcessCaasFile data file', testData.runTimeParquetFileAdd);

      await test.step(`Given 1 participant is processed to cohort`, async () => {
        await validateSqlDatabaseFromAPI(request, testData.checkInDatabaseAdd);
      });

      await test.step(`When same ADD participant record is AMENDED via storage for ${testData.nhsNumberAmend}`, async () => {
        await processFileViaStorage(testData.runTimeParquetFileAmend);
      });

      await test.step(`Then the record should be amended in the cohort`, async () => {
        await validateSqlDatabaseFromAPI(request, testData.checkInDatabaseAmend);
      });
  })

  testWithAmended('@DTOSS-5418-01 @Validate_GP_practice_code_empty_and_reason_for_removal_fields_AMENDED_noException', {
    annotation: {
      type: 'Requirement',
      description: 'Tests - https://nhsd-jira.digital.nhs.uk/browse/DTOSS-2759',
    },
  }, async ({ request, testData }) => {
    await test.step(`Then ADD record should be updated in the cohort`, async () => {
      await validateSqlDatabaseFromAPI(request, testData.checkInDatabaseAdd);
    });

    await test.step(`When same ADD participant record is AMENDED via storage for ${testData.nhsNumberAmend}`, async () => {
      await processFileViaStorage(testData.runTimeParquetFileAmend);
    });

    await test.step(`Then the record should not end up in exception management`, async () => {
      await validateSqlDatabaseFromAPI(request, testData.checkInDatabaseAmend);
    });

  });

  testWithAmended('@DTOSS-4561-01 @Validate_GP_practice_code_empty_and_reason_for_removal_fields_AMENDED_Exception', {
    annotation: {
      type: 'Requirement',
      description: 'Tests - https://nhsd-jira.digital.nhs.uk/browse/DTOSS-2759',
    },
  }, async ({ request, testData }) => {
    await test.step(`Then ADD record should be updated in the cohort`, async () => {
      await validateSqlDatabaseFromAPI(request, testData.checkInDatabaseAdd);
    });

    await test.step(`When same ADD participant record is AMENDED via storage for ${testData.nhsNumberAmend}`, async () => {
      await processFileViaStorage(testData.runTimeParquetFileAmend);
    });

    await test.step(`Then the record should end up in exception management`, async () => {
      await validateSqlDatabaseFromAPI(request, testData.checkInDatabaseAmend);
    });

  });

  testWithAmended('@DTOSS-4329-01 Validation current posting effective date throw exception when invalid date format given for update participants', {
    annotation: {
      type: 'Requirement',
      description: 'Tests - https://nhsd-jira.digital.nhs.uk/browse/DTOSS-3136',
    },
  }, async ({ request, testData }) => {
    await test.step(`Given 3 ADD participants are processed to cohort`, async () => {
      await validateSqlDatabaseFromAPI(request, testData.checkInDatabaseAdd);
    });
    await test.step(`And 3 ADD participants are AMENDED with invalid effective date `, async () => {
      await processFileViaStorage(testData.runTimeParquetFileAmend);
    });
    await test.step(`Then Exception table should have expected rule id and description for 3 AMENDED participants`, async () => {
      await validateSqlDatabaseFromAPI(request, testData.checkInDatabaseAmend);
    });
  })

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

  testWithAmended('@DTOSS-4090-01 Validate existing participant null GP practice code', {
      annotation: {
        type: 'Requirement',
        description: 'Tests - https://nhsd-jira.digital.nhs.uk/browse/DTOSS-4090',
      },
    }, async ({ request, testData }) => {

      await test.step(`When ADD participant is processed via storage`, async () => {
        await processFileViaStorage(testData.runTimeParquetFileAdd);
      });

      await verifyBlobExists('Verify ProcessCaasFile data file', testData.runTimeParquetFileAdd);

      await test.step(`Given 1 participant is processed to cohort`, async () => {
        await validateSqlDatabaseFromAPI(request, testData.checkInDatabaseAdd);
      });

      await test.step(`When same ADD participant record is AMENDED with an invalid GP code via storage for ${testData.nhsNumberAmend}`, async () => {
        await processFileViaStorage(testData.runTimeParquetFileAmend);
      });

      await test.step(`Then the record should not be amended in the cohort`, async () => {
        await validateSqlDatabaseFromAPI(request, testData.checkInDatabaseAmend);
      });
  })

  testWithAmended('@DTOSS-4095-01 Validate incompatible value reason for removal exception', {
      annotation: {
        type: 'Requirement',
        description: 'Tests - https://nhsd-jira.digital.nhs.uk/browse/DTOSS-4095',
      },
    }, async ({ request, testData }) => {

      await test.step(`When ADD participant is processed via storage`, async () => {
        await processFileViaStorage(testData.runTimeParquetFileAdd);
      });

      await verifyBlobExists('Verify ProcessCaasFile data file', testData.runTimeParquetFileAdd);

      await test.step(`Given 1 participant is processed to participant table`, async () => {
        await validateSqlDatabaseFromAPI(request, testData.checkInDatabaseAdd);
      });

      await test.step(`When same ADD participant record  ${testData.nhsNumberAmend}`, async () => {
        await processFileViaStorage(testData.runTimeParquetFileAmend);
      });

      await test.step(`Then the correct exception is displayed in the Exception table`, async () => {
        await validateSqlDatabaseFromAPI(request, testData.checkInDatabaseAmend);
      });
  })

  testWithAmended('@DTOSS-4094-01 Existing Participant Null Reason for Removal Exception', {
      annotation: {
        type: 'Requirement',
        description: 'Tests - https://nhsd-jira.digital.nhs.uk/browse/DTOSS-4094',
      },
    }, async ({ request, testData }) => {

      await test.step(`When ADD participant is processed via storage`, async () => {
        await processFileViaStorage(testData.runTimeParquetFileAdd);
      });

      await verifyBlobExists('Verify ProcessCaasFile data file', testData.runTimeParquetFileAdd);

      await test.step(`Given 1 participant is processed to participant table`, async () => {
        await validateSqlDatabaseFromAPI(request, testData.checkInDatabaseAdd);
      });

      await test.step(`When same ADD participant record  ${testData.nhsNumberAmend}`, async () => {
        await processFileViaStorage(testData.runTimeParquetFileAmend);
      });

      await test.step(`Then the correct exception is displayed in the Exception table`, async () => {
        await validateSqlDatabaseFromAPI(request, testData.checkInDatabaseAmend);
      });
  })
});
