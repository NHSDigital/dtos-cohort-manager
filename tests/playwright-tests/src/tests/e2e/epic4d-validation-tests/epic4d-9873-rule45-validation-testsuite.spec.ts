import { expect, test } from '@playwright/test';
import { cleanupDatabaseFromAPI, getApiTestData, processFileViaStorage, validateSqlDatabaseFromAPI } from '../../steps/steps';
import { createParquetFromJson } from '../../../parquet/parquet-multiplier';
import { config } from "../../../config/env";
import { getRecordsFromBsSelectRetrieveCohort } from '../../../api/distributionService/bsSelectService';
import { composeValidators, expectStatus, validateResponseByStatus } from '../../../api/responseValidators';
import { getRecordsFromExceptionService } from '../../../api/dataService/exceptionService';

test.describe.serial(' @e2e @epic4d-validation-tests validate rule 45', async () => {
    test('@DTOSS-9873-01 200 - @TC1_SIT Verify that a record with a valid GP Practice code does not trigger rule 45', async ({ request }, testInfo) => {
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
        
            const response = await getRecordsFromBsSelectRetrieveCohort(request, { rowCount: 10 });
        
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

    test('@DTOSS-9873-02 - @TC1_SIT Verify that records with an invalid GP Practice code triggers rule 45', async ({ request }, testInfo) => {
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
        
        
        await test.step(`And participants received from exception service api should be 4 with status code of 200`, async () => {
            const ExpectedRowCount = 4;
        
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
})