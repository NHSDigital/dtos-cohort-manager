import { test } from '@playwright/test';
import { cleanupDatabase, validateSqlData } from '../../database/sqlVerifier';
import { processFileViaStorage, validateSqlDatabase } from '../steps/steps'


test.describe('Smoke Tests', () => {
  test('@smoke @DTOSS-6256 Verify file upload and cohort distribution process for ADD', async () => {

    const nhsNumbers = ['1111110662', '2222211794']; // TODO use data generator utility to pass nhs numbers in run-time

    await cleanupDatabase(nhsNumbers); //TODO re-think try remove-participant api instead
    await processFileViaStorage('ADD_2_RECORDS_-_CAAS_BREAST_SCREENING_COHORT.parquet');

    await test.step('Validation in Cohort', async () => {
      //TODO move to validation.json
      const checkInDatabase = {
        "validations": [{
          "validations": {
            "tableName": "BS_COHORT_DISTRIBUTION",
            "columnName": "NHS_Number",
            "columnValue": nhsNumbers[0]
          }
        }, {
          "validations": {
            "tableName": "BS_COHORT_DISTRIBUTION",
            "columnName": "NHS_Number",
            "columnValue": nhsNumbers[1]
          }
        },
        {
          "validations": {
            "tableName": "PARTICIPANT_MANAGEMENT",
            "columnName": "NHS_Number",
            "columnValue": nhsNumbers[0],
            "columnName2": "EXCEPTION_FLAG",
            "columnValue2": "0"
          }
        }, {
          "validations": {
            "tableName": "PARTICIPANT_MANAGEMENT",
            "columnName": "NHS_Number",
            "columnValue": nhsNumbers[1],
            "columnName2": "EXCEPTION_FLAG",
            "columnValue2": "0"
          }
        }
        ]
      }
      await validateSqlDatabase(checkInDatabase.validations);
    });
  });

  test.only('@smoke @DTOSS-6257 Verify file upload and cohort distribution process for ADD followed by AMENDED records', async () => {
    const nhsNumbers = ['2312514176'];
    await cleanupDatabase(nhsNumbers); //TODO move to before this test

    await processFileViaStorage('ADD1_1B8F53_-_CAAS_BREAST_SCREENING_COHORT.parquet');

    await test.step('Validation in Cohort For ADD', async () => {
      const checkInDatabaseForNHSNumber = {
        "validations": [
          {
            "validations": {
              "tableName": "BS_COHORT_DISTRIBUTION",
              "columnName": "NHS_Number",
              "columnValue": nhsNumbers[0]
            }
          }
        ]
      }
      await validateSqlData(checkInDatabaseForNHSNumber.validations);
    });



    await processFileViaStorage(`AMENDED1_1B8F53_-_CAAS_BREAST_SCREENING_COHORT.parquet`);

    test.step(`Validation in Cohort For AMEND followed by ADD`, async () => {

      //DB Validation
      const checkInDatabase = {
        "validations": [
          {
            "validations": {
              "tableName": "BS_COHORT_DISTRIBUTION",
              "columnName": "NHS_Number",
              "columnValue": nhsNumbers[0]
            }
          }, {
            "validations": {
              "tableName": "BS_COHORT_DISTRIBUTION",
              "columnName": "GIVEN_NAME",
              "columnValue": "AMENDEDNewTest1"
            }
          }
        ]
      }
      await validateSqlDatabase(checkInDatabase.validations);
    })

  });

  test('@smoke @DTOSS-7960 Verify GP Practice Code Exception, flag in participant management is set to 1', async () => {

    const nhsNumbers = ['2612314172'];

    await cleanupDatabase(nhsNumbers); //TODO move to before this test
    await processFileViaStorage(`Exception_1B8F53_-_CAAS_BREAST_SCREENING_COHORT.parquet`);

    test.step(`Validation in Cohort For ADD`, async () => {
      const checkInDatabase = {
        "validations": [
          {
            "validations": {
              "tableName": "PARTICIPANT_MANAGEMENT",
              "columnName": "NHS_Number",
              "columnValue": nhsNumbers[0],
              "columnName2": "EXCEPTION_FLAG",
              "columnValue2": "1"
            }
          }
        ]
      }
      await validateSqlDatabase(checkInDatabase.validations);
    })
  });
})

