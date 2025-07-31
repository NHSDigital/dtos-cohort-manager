import { expect, test } from '@playwright/test';
import { cleanupDatabaseFromAPI, getApiTestData, processFileViaStorage, validateSqlDatabaseFromAPI } from '../../steps/steps';
import { createParquetFromJson } from '../../../parquet/parquet-multiplier';
import { config } from "../../../config/env";
import { composeValidators, expectStatus, validateResponseByStatus } from '../../../api/responseValidators';
import { getRecordsFromExceptionService } from '../../../api/dataService/exceptionService';
import { getRecordsFromBsSelectRetrieveCohort } from '../../../api/distributionService/bsSelectService';

test.describe.serial(' @e2e @epic4d-validation-tests validate rule 45', async () => {
    test('@DTOSS-A355-01 @TC1_SIT Verify that record with an ENG current posting and an invalid GP Practice code triggers rule 45', async ({ request }, testInfo) => {
        const [checkInDatabase, inputParticipantRecord, nhsNumbers, testFilesPath] = await getApiTestData(testInfo.title);
        
        await cleanupDatabaseFromAPI(request, nhsNumbers);
        
        const parquetFile = await createParquetFromJson(nhsNumbers, inputParticipantRecord, testFilesPath);
        
        await test.step(`When 1 ADD participant is processed via storage`, async () => {
            await processFileViaStorage(parquetFile);
        });
        
        await test.step(`Then participants should be updated in the cohort ready to be picked up`, async () => {
            await validateSqlDatabaseFromAPI(request, checkInDatabase);
            await new Promise(resolve => setTimeout(resolve, config.apiWaitTime));
        });
        
        
        await test.step(`And participants received from exception service api should be 3 with status code of 200`, async () => {
            // There should be exceptions for rule 45 and 3601, plus an exception to say that a participant cannot be added to cohort distribution as there is an exception
            const ExpectedRowCount = 3; 

            const response = await getRecordsFromExceptionService(request);
        
            const genericValidations = composeValidators(
                expectStatus(200),
                validateResponseByStatus()
            );
            await genericValidations(response);
            //Extend custom assertions
            expect(Array.isArray(response.data)).toBeTruthy();
            expect(response.data.length).toBe(ExpectedRowCount);
        });
    });

    test('@DTOSS-A356-01 @TC1_SIT Verify that record with an IM current posting and an invalid GP Practice code triggers rule 45', async ({ request }, testInfo) => {
        const [checkInDatabase, inputParticipantRecord, nhsNumbers, testFilesPath] = await getApiTestData(testInfo.title);
        
        await cleanupDatabaseFromAPI(request, nhsNumbers);
        
        const parquetFile = await createParquetFromJson(nhsNumbers, inputParticipantRecord, testFilesPath);
        
        await test.step(`When 1 ADD participant is processed via storage`, async () => {
            await processFileViaStorage(parquetFile);
        });
        
        await test.step(`Then participants should be updated in the cohort ready to be picked up`, async () => {
            await validateSqlDatabaseFromAPI(request, checkInDatabase);
            await new Promise(resolve => setTimeout(resolve, config.apiWaitTime));
        });
        
        
        await test.step(`And participants received from exception service api should be 2 with status code of 200`, async () => {
            // There should be an exception for rule 45 and an exception to say that a participant cannot be added to cohort distribution as there is an exception
            const ExpectedRowCount = 2; 

            const response = await getRecordsFromExceptionService(request);
        
            const genericValidations = composeValidators(
                expectStatus(200),
                validateResponseByStatus()
            );
            await genericValidations(response);
            //Extend custom assertions
            expect(Array.isArray(response.data)).toBeTruthy();
            expect(response.data.length).toBe(ExpectedRowCount);
        });
    });

    test('@DTOSS-A357-01 - @TC1_SIT Verify that record with a DMS current posting and an invalid GP Practice code triggers rule 45', async ({ request }, testInfo) => {
        const [checkInDatabase, inputParticipantRecord, nhsNumbers, testFilesPath] = await getApiTestData(testInfo.title);
        
        await cleanupDatabaseFromAPI(request, nhsNumbers);
        
        const parquetFile = await createParquetFromJson(nhsNumbers, inputParticipantRecord, testFilesPath);
        
        await test.step(`When 1 ADD participant is processed via storage`, async () => {
            await processFileViaStorage(parquetFile);
        });
        
        await test.step(`Then participants should be updated in the cohort ready to be picked up`, async () => {
            await validateSqlDatabaseFromAPI(request, checkInDatabase);
            await new Promise(resolve => setTimeout(resolve, config.apiWaitTime));
        });
        
        
        await test.step(`And participants received from exception service api should be 3 with status code of 200`, async () => {
            // There should be exceptions for rule 45 and 3601, plus an exception to say that a participant cannot be added to cohort distribution as there is an exception
            const ExpectedRowCount = 3; 

            const response = await getRecordsFromExceptionService(request);
        
            const genericValidations = composeValidators(
                expectStatus(200),
                validateResponseByStatus()
            );
            await genericValidations(response);
            //Extend custom assertions
            expect(Array.isArray(response.data)).toBeTruthy();
            expect(response.data.length).toBe(ExpectedRowCount);
        });
    });

    test('@DTOSS-A358-01 200 - @TC1_SIT Verify that a record with an ENG current posting and a valid GP Practice code does not trigger rule 45', async ({ request }, testInfo) => {
        const [checkInDatabase, inputParticipantRecord, nhsNumbers, testFilesPath] = await getApiTestData(testInfo.title);
        
        await cleanupDatabaseFromAPI(request, nhsNumbers);
        
        const parquetFile = await createParquetFromJson(nhsNumbers, inputParticipantRecord, testFilesPath);
        
        await test.step(`When 1 ADD participant is processed via storage`, async () => {
            await processFileViaStorage(parquetFile);
        });
        
        await test.step(`Then participants should be updated in the cohort ready to be picked up`, async () => {
            await validateSqlDatabaseFromAPI(request, checkInDatabase);
            await new Promise(resolve => setTimeout(resolve, config.apiWaitTime));
        });
        
        await test.step(`And participants received from api should be 1 with status code of 200`, async () => {
            const ExpectedRowCount = 1;
        
            const response = await getRecordsFromBsSelectRetrieveCohort(request, { rowCount: 1 });
        
            const genericValidations = composeValidators(
                expectStatus(200),
                validateResponseByStatus()
            );
            await genericValidations(response);
            //Extend custom assertions
            expect(Array.isArray(response.data)).toBeTruthy();
            expect(response.data.length).toBe(ExpectedRowCount);
        });
    });

    test('@DTOSS-A359-01 200 - @TC1_SIT Verify that a record with a DMS current posting and an excluded GP Practice code does not trigger rule 45', async ({ request }, testInfo) => {
        const [checkInDatabase, inputParticipantRecord, nhsNumbers, testFilesPath] = await getApiTestData(testInfo.title);
        
        await cleanupDatabaseFromAPI(request, nhsNumbers);
        
        const parquetFile = await createParquetFromJson(nhsNumbers, inputParticipantRecord, testFilesPath);
        
        await test.step(`When 1 ADD participant is processed via storage`, async () => {
            await processFileViaStorage(parquetFile);
        });
        
        await test.step(`Then participants should be updated in the cohort ready to be picked up`, async () => {
            await validateSqlDatabaseFromAPI(request, checkInDatabase);
            await new Promise(resolve => setTimeout(resolve, config.apiWaitTime));
        });

        await test.step(`And there should be no participants received from exception service api with an error code of Rule 45`, async () => {
            const response = await getRecordsFromExceptionService(request);
        
            const genericValidations = composeValidators(
                expectStatus(200),
                validateResponseByStatus()
            );
            await genericValidations(response);
            //Extend custom assertions
            expect(Array.isArray(response.data)).toBeTruthy();
            const rule45Found = response.data.includes('Rule 45.');
            expect(rule45Found).toBe(false);
        }); 
    });

    test('@DTOSS-A360-01 200 - @TC1_SIT Verify that a record where 2 or more conditions are not met does not trigger rule 45', async ({ request }, testInfo) => {
        const [checkInDatabase, inputParticipantRecord, nhsNumbers, testFilesPath] = await getApiTestData(testInfo.title);
        
        await cleanupDatabaseFromAPI(request, nhsNumbers);
        
        const parquetFile = await createParquetFromJson(nhsNumbers, inputParticipantRecord, testFilesPath);
        
        await test.step(`When 1 ADD participant is processed via storage`, async () => {
            await processFileViaStorage(parquetFile);
        });
        
        await test.step(`Then participants should be updated in the cohort ready to be picked up`, async () => {
            await validateSqlDatabaseFromAPI(request, checkInDatabase);
            await new Promise(resolve => setTimeout(resolve, config.apiWaitTime));
        });
        
        await test.step(`And participants received from api should be 1 with status code of 200`, async () => {
            const ExpectedRowCount = 1;
        
            const response = await getRecordsFromBsSelectRetrieveCohort(request, { rowCount: 1 });
        
            const genericValidations = composeValidators(
                expectStatus(200),
                validateResponseByStatus()
            );
            await genericValidations(response);
            //Extend custom assertions
            expect(Array.isArray(response.data)).toBeTruthy();
            expect(response.data.length).toBe(ExpectedRowCount);
        });
    });
            
            

})