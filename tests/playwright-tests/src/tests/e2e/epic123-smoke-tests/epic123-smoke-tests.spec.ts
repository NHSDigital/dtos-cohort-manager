import { test } from '@playwright/test';
import { cleanupDatabaseFromAPI, getTestData, processFileViaStorage, validateSqlDatabaseFromAPI } from '../../steps/steps'
import { createParquetFromJson } from '../../../parquet/parquet-multiplier';



test.describe.parallel('Cohort Tests', () => {

  test('@DTOSS-6256-01 Verify file upload and cohort distribution process for ADD', {
    tag: ['@smoke @e2e', '@ds'],
  }, async ({ request }, testInfo) => {
    const [checkInDatabase, nhsNumbers, parquetFile, inputParticipantRecord, testFilesPath] = await getTestData(testInfo.title, "ADD", true);

    let runTimeParquetFile: string = "";
    if (!parquetFile) {
      runTimeParquetFile = await createParquetFromJson(nhsNumbers, inputParticipantRecord!, testFilesPath!, "ADD", false);
    }
    await test.step(`Given database does not contain 2 ADD records that will be processed`, async () => {
      await cleanupDatabaseFromAPI(request, nhsNumbers);
    });

    await test.step(`When 2 ADD participants are processed via storage`, async () => {
      await processFileViaStorage(runTimeParquetFile);
    });

    await test.step(`Then NHS Numbers should be updated in the cohort`, async () => {
      await validateSqlDatabaseFromAPI(request, checkInDatabase);
    });

  });

  test('@DTOSS-6257-01 Verify file upload and cohort distribution process for ADD followed by AMENDED records', {
    tag: ['@smoke @e2e', '@ds'],
  }, async ({ request }, testInfo) => {

    const [checkInDatabaseAdd, nhsNumbers, parquetFile, inputParticipantRecord, testFilesPath] = await getTestData(testInfo.title, "ADD", true);

    let runTimeParquetFileAdd: string = "";
    if (!parquetFile) {
      runTimeParquetFileAdd = await createParquetFromJson(nhsNumbers, inputParticipantRecord!, testFilesPath!, "ADD", false);
    }


    await test.step(`Given database does not contain record that will be processed`, async () => {
      await cleanupDatabaseFromAPI(request, nhsNumbers);
    });

    await test.step(`When ADD participant is processed via storage`, async () => {
      await processFileViaStorage(runTimeParquetFileAdd);
    });

    await test.step(`Then ADD record should be updated in the cohort`, async () => {
      await validateSqlDatabaseFromAPI(request, checkInDatabaseAdd);
    });


    const [checkInDatabaseAmend, nhsNumberAmend, parquetFileAmend, inputParticipantRecordAmend, testFilesPathAmend] = await getTestData(testInfo.title, "AMENDED", true);

    let runTimeParquetFileAmend: string = "";
    if (!parquetFileAmend) {
      runTimeParquetFileAmend = await createParquetFromJson(nhsNumberAmend, inputParticipantRecordAmend!, testFilesPathAmend!, "AMENDED", false);
    }


    await test.step(`When same ADD participant record is AMENDED via storage`, async () => {
      await processFileViaStorage(runTimeParquetFileAmend);
    });

    await test.step(`Then AMENDED record name should be updated in the cohort: ${nhsNumberAmend}`, async () => {
      await validateSqlDatabaseFromAPI(request, checkInDatabaseAmend);
    });

  });

  test('@DTOSS-6407-01 Verify file upload handles EmptyDOB Exception', {
    tag: ['@smoke @e2e', '@ds'],
  }, async ({ request }, testInfo) => {

    const [checkInDatabaseAdd, nhsNumbers, parquetFile, inputParticipantRecord, testFilesPath] = await getTestData(testInfo.title, "ADD", true);

    let runTimeParquetFileAdd: string = "";
    if (!parquetFile) {
      runTimeParquetFileAdd = await createParquetFromJson(nhsNumbers, inputParticipantRecord!, testFilesPath!, "ADD", false);
    }

    await test.step(`Given database does not contain record that will be processed`, async () => {
      await cleanupDatabaseFromAPI(request, nhsNumbers);
    });

    await test.step(`When ADD participant is processed via storage`, async () => {
      await processFileViaStorage(runTimeParquetFileAdd);
    });

    await test.step(`Then ADD record should be updated in the cohort`, async () => {
      await validateSqlDatabaseFromAPI(request, checkInDatabaseAdd);
    });


  const [checkInDatabaseAmend, nhsNumberAmend, parquetFileAmend, inputParticipantRecordAmend, testFilesPathAmend] = await getTestData(testInfo.title, "AMENDED", true);

  let runTimeParquetFileAmend: string = "";
  if (!parquetFileAmend) {
    runTimeParquetFileAmend = await createParquetFromJson(nhsNumberAmend, inputParticipantRecordAmend!, testFilesPathAmend!, "AMENDED", false);
  }


    await test.step(`When same ADD participant record is AMENDED via storage for ${nhsNumberAmend}`, async () => {
      await processFileViaStorage(runTimeParquetFileAmend);
    });

    await test.step(`Then the Exception table should contain the below details for the NHS Number`, async () => {
      await validateSqlDatabaseFromAPI(request, checkInDatabaseAmend);
    });

  });

});


test.describe.parallel('Exception Tests', () => {

  test('@DTOSS-6406-01 Verify file upload handles invalid GP Practice Code Exception', {
    tag: ['@smoke @e2e', '@ds'],
  }, async ({ request }, testInfo) => {

    const [checkInDatabase, nhsNumbers, parquetFile, inputParticipantRecord, testFilesPath] = await getTestData(testInfo.title, "ADD", true);

      let runTimeParquetFile: string = "";
      if (!parquetFile) {
        runTimeParquetFile = await createParquetFromJson(nhsNumbers, inputParticipantRecord!, testFilesPath!, "ADD", false);
      }

    await test.step(`Given database does not contain record that will be processed`, async () => {
      await cleanupDatabaseFromAPI(request, nhsNumbers);
    });

    await test.step(`When ADD participant is processed via storage`, async () => {
      await processFileViaStorage(runTimeParquetFile);
    });

    await test.step(`Then the Exception table should contain the below details for the NHS Number`, async () => {
      await validateSqlDatabaseFromAPI(request, checkInDatabase);
    });
  });

  test('@DTOSS-7960-01 Verify GP Practice Code Exception flag in participant management set to 1', {
    tag: ['@smoke @e2e', '@ds'],
  }, async ({ request }, testInfo) => {
    console.info(`Running test: ${testInfo.title}`);

    const [checkInDatabase, nhsNumbers, parquetFile, inputParticipantRecord, testFilesPath] = await getTestData(testInfo.title, "ADD", true);

    let runTimeParquetFile: string = "";
    if (!parquetFile) {
      runTimeParquetFile = await createParquetFromJson(nhsNumbers, inputParticipantRecord!, testFilesPath!, "ADD", false);
    }

    await test.step(`Given database does not contain records that will be processed: ${nhsNumbers}  `, async () => {
      await cleanupDatabaseFromAPI(request, nhsNumbers);
    });

    await test.step(`When participants are processed via storage`, async () => {
      await processFileViaStorage(runTimeParquetFile);
    });

    await test.step(`Then records should be updated in the cohort`, async () => {
      await validateSqlDatabaseFromAPI(request, checkInDatabase);
    });
  });

});



