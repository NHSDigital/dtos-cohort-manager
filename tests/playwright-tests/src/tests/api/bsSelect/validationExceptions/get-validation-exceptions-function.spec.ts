// import { test, expect } from '@playwright/test';
// import { getApiTestData, cleanupDatabaseFromAPI, validateSqlDatabaseFromAPI } from '../../../steps/steps';

// interface ValidationException {
//   EXCEPTION_ID: number;
//   FILE_NAME: string;
//   NHS_NUMBER: string;
//   DATE_CREATED: string;
//   DATE_RESOLVED: string | null;
//   RULE_ID: number;
//   RULE_DESCRIPTION: string;
//   ERROR_RECORD: string;
//   CATEGORY: number;
//   SCREENING_NAME: string;
//   EXCEPTION_DATE: string;
//   COHORT_NAME: string;
//   IS_FATAL: number;
//   SERVICENOW_ID: string | null;
//   SERVICENOW_CREATED_DATE: string;
//   RECORD_UPDATED_DATE: string;
// }

// interface ValidationExceptionsData {
//   validationExceptions: ValidationException[];
// }

// test.describe.serial('@regression @api @validation-exceptions GetValidationExceptions Mock Data Tests', () => {
//   test('@DTOSS-9609-01 - Verify validation_exceptions_sample_data contains all 10 validation exception items', async ({ request }, testInfo) => {
//     const [, validationExceptions, nhsNumbers] = await getApiTestData(testInfo.title, 'validation_exceptions_sample_data');

//     await test.step('Cleanup database using data services', async () => {
//       await cleanupDatabaseFromAPI(request, nhsNumbers);
//     });

//     await test.step('Verify test data contains expected 10 validation exceptions', async () => {
//       const validationExceptionsData = validationExceptions as ValidationExceptionsData;
//       const validationExceptions: ValidationException[] = validationExceptionsData.validationExceptions || validationExceptions as ValidationException[];

//       expect(validationExceptions.length).toBe(10);

//       const datasetNhsNumbers: string[] = validationExceptions.map((item: ValidationException) => item.NHS_NUMBER);
//       nhsNumbers.forEach((expectedNhs: string) => {
//         expect(datasetNhsNumbers).toContain(expectedNhs);
//       });
//     });
//   });
// });





import { test, expect } from '@playwright/test';
import { getApiTestData } from '../../../steps/steps';
import * as fs from 'fs';
import * as path from 'path';

test.describe.serial('@regression @api @validation-exceptions GetValidationExceptions Mock Data Tests', () => {
  test('@DTOSS-9609-01 - Verify validation_exceptions_sample_data contains all 10 validation exception items', async ({}, testInfo) => {
    const [validations, validationExceptions, nhsNumbers, testFilesPath] = await getApiTestData(testInfo.title, 'validation_exceptions_sample_data');

    await test.step('Verify test data contains expected 10 validation exceptions', async () => {
      const jsonFile = fs.readdirSync(testFilesPath).find(fileName => fileName.endsWith('.json') && fileName.startsWith('validation_exceptions_sample_data'));
      const parsedData = JSON.parse(fs.readFileSync(path.join(testFilesPath, jsonFile!), 'utf-8'));

      const validationExceptions = parsedData.validationExceptions;

      expect(Array.isArray(validationExceptions)).toBe(true);
      expect(validationExceptions.length).toBe(10);
    });
  });
});
