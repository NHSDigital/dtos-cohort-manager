import { test, expect, APIRequestContext, TestInfo } from "@playwright/test";
import { TestHooks } from "../../hooks/test-hooks";
import { cleanupDatabaseFromAPI, getApiTestData, processFileViaStorage, validateSqlDatabaseFromAPI } from "../../steps/steps";
import { createParquetFromJson } from "../../../parquet/parquet-multiplier";
import { getRecordsFromCohortDistributionService } from "../../../api/dataService/cohortDistributionService";
import { getValidationExceptions } from "../../../api/dataService/exceptionService";

annotation: [{
  type: 'Requirement',
  description: 'Tests - https://nhsd-jira.digital.nhs.uk/browse/DTOSS-9935'
}]

test.describe('@regression @e2e @epic4-validation-test Rule 8 Tests', () => {

    TestHooks.setupAllTestHooks();

    test('@DTOSS-A451-01 - AC1 - Verify participant is in CohortDistribution and that a transformation rule 8 exists in Exception table', async ({ request }: { request: APIRequestContext }, testInfo: TestInfo) => {
        const [checkInDatabase, inputParticipantRecord, nhsNumbers, testFilesPath] = await getApiTestData(testInfo.title);
        
        await cleanupDatabaseFromAPI(request, nhsNumbers);
        
        const parquetFile = await createParquetFromJson(nhsNumbers, inputParticipantRecord, testFilesPath);
        
        await test.step(`When I ADD participant is processed via storage`, async () => {
          await processFileViaStorage(parquetFile);
        });

        await test.step('Then participant should be in cohort distribution', async ()=> {
          const cohortResponse = await getRecordsFromCohortDistributionService(request);
          if (!cohortResponse || !Array.isArray(cohortResponse.data)) {
            throw new Error('No data returned from cohort distribution service');
          }
        })

        await test.step('Then participant should raise a rule 8 transformation exception', async ()=> {
          const exceptionResponse = await getValidationExceptions(request, {exceptionCategory: 8});
                expect(Array.isArray(exceptionResponse.data) && exceptionResponse.data.length === 1).toBe(true);
        })


      })

      test('@DTOSS-A452-01 - AC1 - Verify participant is in CohortDistribution table with no exceptions', async ({ request }: { request: APIRequestContext }, testInfo: TestInfo) => {
        const [checkInDatabase, inputParticipantRecord, nhsNumbers, testFilesPath] = await getApiTestData(testInfo.title);
        
        await cleanupDatabaseFromAPI(request, nhsNumbers);
        
        const parquetFile = await createParquetFromJson(nhsNumbers, inputParticipantRecord, testFilesPath);
        
        await test.step(`When I ADD participant is processed via storage`, async () => {
          await processFileViaStorage(parquetFile);
        });

        await test.step('Then participant should be in cohort distribution', async ()=> {
          const cohortResponse = await getRecordsFromCohortDistributionService(request);
          if (!cohortResponse || !Array.isArray(cohortResponse.data)) {
            throw new Error('No data returned from cohort distribution service');
          }
        })

        await test.step('Then participant should not raise a transformation exception', async ()=> {
          const response = await getValidationExceptions(request, {exceptionCategory: 8});
                expect(response.data === null || (Array.isArray(response.data) && response.data.length === 0)).toBe(true);
        })


      })

      test('@DTOSS-A453-01 - AC1 - Verify participant is in CohortDistribution and that a transformation rule 8 exists in Exception table', async ({ request }: { request: APIRequestContext }, testInfo: TestInfo) => {
        const [checkInDatabase, inputParticipantRecord, nhsNumbers, testFilesPath] = await getApiTestData(testInfo.title);
        
        await cleanupDatabaseFromAPI(request, nhsNumbers);
        
        const parquetFile = await createParquetFromJson(nhsNumbers, inputParticipantRecord, testFilesPath);
        
        await test.step(`When I ADD participant is processed via storage`, async () => {
          await processFileViaStorage(parquetFile);
        });

        await test.step('Then participant should be in cohort distribution', async ()=> {
          const cohortResponse = await getRecordsFromCohortDistributionService(request);
          if (!cohortResponse || !Array.isArray(cohortResponse.data)) {
            throw new Error('No data returned from cohort distribution service');
          }
        })

        await test.step('Then participant should raise a rule 8 transformation exception', async ()=> {
          const exceptionResponse = await getValidationExceptions(request, {exceptionCategory: 8});
                expect(Array.isArray(exceptionResponse.data) && exceptionResponse.data.length === 1).toBe(true);
        })


      })

      test('@DTOSS-A454-01 - AC1 - Verify participant is in CohortDistribution and that a transformation rule 8 exists in Exception table', async ({ request }: { request: APIRequestContext }, testInfo: TestInfo) => {
        const [inputParticipantRecord, nhsNumbers, testFilesPath] = await getApiTestData(testInfo.title);
        
        await cleanupDatabaseFromAPI(request, nhsNumbers);
        
        const parquetFile = await createParquetFromJson(nhsNumbers, inputParticipantRecord, testFilesPath);
        
        await test.step(`When I ADD participant is processed via storage`, async () => {
          await processFileViaStorage(parquetFile);
        });

        await test.step('Then participant should be in cohort distribution', async ()=> {
          const cohortResponse = await getRecordsFromCohortDistributionService(request);
          if (!cohortResponse || !Array.isArray(cohortResponse.data)) {
            throw new Error('No data returned from cohort distribution service');
          }
        })

        await test.step('Then participant should raise a rule 8 transformation exception', async ()=> {
          const exceptionResponse = await getValidationExceptions(request, {exceptionCategory: 8});
                expect(Array.isArray(exceptionResponse.data) && exceptionResponse.data.length === 1).toBe(true);
        })


      })


      test('@DTOSS-A455-01 - AC1 - Verify participant is in CohortDistribution table with no exceptions', async ({ request }: { request: APIRequestContext }, testInfo: TestInfo) => {
        const [checkInDatabase, inputParticipantRecord, nhsNumbers, testFilesPath] = await getApiTestData(testInfo.title);
        
        await cleanupDatabaseFromAPI(request, nhsNumbers);
        
        const parquetFile = await createParquetFromJson(nhsNumbers, inputParticipantRecord, testFilesPath);
        
        await test.step(`When I ADD participant is processed via storage`, async () => {
          await processFileViaStorage(parquetFile);
        });

        await test.step('Then participant should be in cohort distribution', async ()=> {
          const cohortResponse = await getRecordsFromCohortDistributionService(request);
          if (!cohortResponse || !Array.isArray(cohortResponse.data)) {
            throw new Error('No data returned from cohort distribution service');
          }
        })

        await test.step('Then participant should not raise a transformation exception', async ()=> {
          const response = await getValidationExceptions(request, {exceptionCategory: 8});
                expect(response.data === null || (Array.isArray(response.data) && response.data.length === 0)).toBe(true);
        })

      })

      test('@DTOSS-A456-01 - AC1 - Verify participant is in CohortDistribution and that a transformation rule 8 exists in Exception table', async ({ request }: { request: APIRequestContext }, testInfo: TestInfo) => {
        const [inputParticipantRecord, nhsNumbers, testFilesPath] = await getApiTestData(testInfo.title);
        
        await cleanupDatabaseFromAPI(request, nhsNumbers);
        
        const parquetFile = await createParquetFromJson(nhsNumbers, inputParticipantRecord, testFilesPath);
        
        await test.step(`When I ADD participant is processed via storage`, async () => {
          await processFileViaStorage(parquetFile);
        });

        await test.step('Then participant should be in cohort distribution', async ()=> {
          const cohortResponse = await getRecordsFromCohortDistributionService(request);
          if (!cohortResponse || !Array.isArray(cohortResponse.data)) {
            throw new Error('No data returned from cohort distribution service');
          }
        })

        await test.step('Then participant should raise a rule 8 transformation exception', async ()=> {
          const exceptionResponse = await getValidationExceptions(request, {exceptionCategory: 8});
                expect(Array.isArray(exceptionResponse.data) && exceptionResponse.data.length === 1).toBe(true);
        })


      })

      test('@DTOSS-A457-01 - AC1 - Verify participant is in CohortDistribution table with no exceptions', async ({ request }: { request: APIRequestContext }, testInfo: TestInfo) => {
        const [checkInDatabase, inputParticipantRecord, nhsNumbers, testFilesPath] = await getApiTestData(testInfo.title);
        
        await cleanupDatabaseFromAPI(request, nhsNumbers);
        
        const parquetFile = await createParquetFromJson(nhsNumbers, inputParticipantRecord, testFilesPath);
        
        await test.step(`When I ADD participant is processed via storage`, async () => {
          await processFileViaStorage(parquetFile);
        });

        await test.step('Then participant should be in cohort distribution', async ()=> {
          const cohortResponse = await getRecordsFromCohortDistributionService(request);
          if (!cohortResponse || !Array.isArray(cohortResponse.data)) {
            throw new Error('No data returned from cohort distribution service');
          }
        })

        await test.step('Then participant should not raise a transformation exception', async ()=> {
          const response = await getValidationExceptions(request, {exceptionCategory: 8});
                expect(response.data === null || (Array.isArray(response.data) && response.data.length === 0)).toBe(true);
        })

      })

      test('@DTOSS-A458-01 - AC1 - Verify participant is in CohortDistribution and that a transformation rule 8 exists in Exception table', async ({ request }: { request: APIRequestContext }, testInfo: TestInfo) => {
        const [inputParticipantRecord, nhsNumbers, testFilesPath] = await getApiTestData(testInfo.title);
        
        await cleanupDatabaseFromAPI(request, nhsNumbers);
        
        const parquetFile = await createParquetFromJson(nhsNumbers, inputParticipantRecord, testFilesPath);
        
        await test.step(`When I ADD participant is processed via storage`, async () => {
          await processFileViaStorage(parquetFile);
        });

        await test.step('Then participant should be in cohort distribution', async ()=> {
          const cohortResponse = await getRecordsFromCohortDistributionService(request);
          if (!cohortResponse || !Array.isArray(cohortResponse.data)) {
            throw new Error('No data returned from cohort distribution service');
          }
        })

        await test.step('Then participant should raise a rule 8 transformation exception', async ()=> {
          const exceptionResponse = await getValidationExceptions(request, {exceptionCategory: 8});
                expect(Array.isArray(exceptionResponse.data) && exceptionResponse.data.length === 1).toBe(true);
        })


      })

      test('@DTOSS-A459-01 - AC1 - Verify participant is in CohortDistribution and that a general exception exists in Exception table', async ({ request }: { request: APIRequestContext }, testInfo: TestInfo) => {
        const [inputParticipantRecord, nhsNumbers, testFilesPath] = await getApiTestData(testInfo.title);
        
        await cleanupDatabaseFromAPI(request, nhsNumbers);
        
        const parquetFile = await createParquetFromJson(nhsNumbers, inputParticipantRecord, testFilesPath);
        
        await test.step(`When I ADD participant is processed via storage`, async () => {
          await processFileViaStorage(parquetFile);
        });

        await test.step('Then participant should be in cohort distribution', async ()=> {
          const cohortResponse = await getRecordsFromCohortDistributionService(request);
          if (!cohortResponse || !Array.isArray(cohortResponse.data)) {
            throw new Error('No data returned from cohort distribution service');
          }
        })

        await test.step('Then participant should raise a general exception', async ()=> {
          const exceptionResponse = await getValidationExceptions(request);
                expect(Array.isArray(exceptionResponse.data) && exceptionResponse.data.length >= 1).toBe(true);
        })


      })
      
})
