import { test } from '@playwright/test';
import { cleanupDatabase, validateSqlData } from '../../database/sqlVerifier';
import { processFileViaStorage, validateSqlDatabase } from '../steps/steps'


test.describe('Smoke Tests', () => {
  test('@smoke @DTOSS-6256 Verify file upload and cohort distribution process for ADD', async () => {

    const nhsNumbers = ['1111110662', '2222211794']; // TODO use data generator utility to pass nhs numbers in run-time

    await test.step(`Given database does not contain 2 ADD records that will be processed: ${nhsNumbers[0]} and ${nhsNumbers[1]}  `, async () => {
      await cleanupDatabase(nhsNumbers); //TODO re-think try remove-participant api instead
    });

    await test.step(`When 2 ADD participants are processed via storage`, async () => {
      await processFileViaStorage('ADD_2_RECORDS_-_CAAS_BREAST_SCREENING_COHORT.parquet');
    });

    await test.step(`Then NHS Numbers should be should be updated in the cohort`, async () => {
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
        }
        ]
      }
      await validateSqlDatabase(checkInDatabase.validations);
    });

  });

  test('@smoke @DTOSS-6257 Verify file upload and cohort distribution process for ADD followed by AMENDED records', async () => {
    const nhsNumbers = ['2312514176'];

    await test.step(`Given database does not contain record that will be processed: ${nhsNumbers[0]}  `, async () => {
      await cleanupDatabase(nhsNumbers); //TODO re-think try remove-participant api instead
    });

    await test.step(`When ADD participant is processed via storage`, async () => {
      await processFileViaStorage('ADD1_1B8F53_-_CAAS_BREAST_SCREENING_COHORT.parquet');
    });

    await test.step(`Then ADD record should be updated in the cohort`, async () => {

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

    await test.step(`When same ADD participant record is AMENDED via storage`, async () => {
      await processFileViaStorage(`AMENDED1_1B8F53_-_CAAS_BREAST_SCREENING_COHORT.parquet`);
    });

    await test.step(`Then AMENDED record name should be updated in the cohort`, async () => {
      //DB Validation
      const checkInDatabase = {
        "validations": [
          {
            "validations": {
              "tableName": "BS_COHORT_DISTRIBUTION",
              "columnName": "GIVEN_NAME",
              "columnValue": "AMENDEDNewTest1",
              "columnName2": "NHS_Number",
              "columnValue2": nhsNumbers[0]
            }
          }
        ]
      }
      await validateSqlDatabase(checkInDatabase.validations);
    });




  });

  test('@smoke @DTOSS-7960 Verify GP Practice Code Exception, flag in participant management is set to 1', async () => {

    const nhsNumbers = ['2612314172'];

    await test.step(`Given database does not contain records that will be processed: ${nhsNumbers}  `, async () => {
      await cleanupDatabase(nhsNumbers); //TODO re-think try remove-participant api instead
    });

    await test.step(`When participants are processed via storage`, async () => {
      await processFileViaStorage(`Exception_1B8F53_-_CAAS_BREAST_SCREENING_COHORT.parquet`);
    });

    await test.step(`Then records should be updated in the cohort`, async () => {
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

    });





  });
})

