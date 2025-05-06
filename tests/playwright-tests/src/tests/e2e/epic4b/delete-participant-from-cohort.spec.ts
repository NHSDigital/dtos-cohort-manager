import { test, expect } from '../../fixtures/test-fixtures'
import { cleanupDatabaseFromAPI, processFileViaStorage, validateSqlDatabaseFromAPI } from '../../steps/steps';
import { filterUsing3PointCheck, getRecordsFromCohortDistributionService } from '../../../api/dataService/cohortDistributionService';

test.fail('@DTOSS-7689-01 @insprint @epic4b Test to Verify that the participant should be deleted data from Cohort Manager when that participant has been marked as blocked and there is a request to delete their data by locating their breast screening specific records in cohort distribution using their NHS ID, Date of Birth and last name (3 point check).', async ({ request, testData }) => {

  await test.step('Given I have a participant that has been marked as blocked AND I have a request to delete their data', async () => {
    await cleanupDatabaseFromAPI(request, testData.nhsNumbers);
    await processFileViaStorage(testData.runTimeParquetFile);
  });

  await test.step('When I locate their breast screening specific records in cohort distribution using their NHS ID, Date of Birth and last name (3 point check)', async () => {
    await validateSqlDatabaseFromAPI(request, testData.checkInDatabase);
    const records = await getRecordsFromCohortDistributionService(request);
    const filteredRecords = await filterUsing3PointCheck(records, testData.inputParticipantRecord);
    console.info(filteredRecords);

  });

  await test.step('Then I should delete this data from Cohort Manager', async () => {
    // ... Add steps; check existing or add new
    /// TODO Integrate new API call to DELETE participant
  });

  await test.step('And The records being deleted should be verified that the NHS ID, Date of Birth and last name all EXACTLY match the request from the IAO', async () => {
    // ... Add steps; check existing or add new

    const records = await getRecordsFromCohortDistributionService(request);
    const filteredRecords = await filterUsing3PointCheck(records, testData.inputParticipantRecord);
    expect(filteredRecords).toHaveLength(0);


  });

});


test('@DTOSS-7689-02 @insprint @epic4b Test to Verify that the participant should be deleted data from Cohort Manager when that participant has been marked as blocked and there is a request to delete their data by locating their breast screening specific records in cohort distribution using their NHS ID, Date of Birth and last name (3 point check).', async ({ request, testData }) => {

  await test.step('Given that I am deleting a participants data from Cohort distribution', async () => {
    // ... Add steps; check existing or add new
    await cleanupDatabaseFromAPI(request, testData.nhsNumbers);
    await processFileViaStorage(testData.runTimeParquetFile);
  });


  await test.step('When data in cohort manager does not match the 3 point check EXACTLY', async () => {
    // ... Add steps; check existing or add new
    await validateSqlDatabaseFromAPI(request, testData.checkInDatabase);
  });

  await test.step('Then I should not delete the data and I should update the IAO accordingly so that they can make a decision on whether to delete the data or not', async () => {
    // ... Add steps; check existing or add new
    //
    //
    ///
    //
    /// TODO Integrate new API call to DELETE participant
    console.info(`Fail this test on purpose to demo traceability of user story to test execution in JIRA`)
    expect(100).toBe(99);
  });



});




