// For Future Epic 4d validation Tests

// Note:
// These tests have been fully migrated to the new and improved test execution orchestration:
//
// - Add:
//   tests/runner/runner-workflow-add
// - Add followed by Amend:
//   tests/runner/runner-workflow-amend
//
// This approach allows bulk loading of test data for all tests before proceeding with validation, instead of loading test data for each test individually.
//
// Guidance:
//
// - First, try adding new tests using the runner.
// - If custom calls are needed, use new file for test addition. - tests\playwright-tests\src\tests\e2e\epic4d-6045-validation-tests\epic4d-6045-validation-tests.spec.ts
//
// Test Tags:
// There is no impact to the test tags. The same tags can be used to trigger these tests.
// For example, to run regression tests, use:
//
//   npm run test:regression_e2e_epic4d
//
// This equates to "@epic4d-" tags, configured in the package.json at the playwright-tests root location.


 export const runnerBasedEpic4dTestScenariosAdd = "@DTOSS-9496-01|@DTOSS-9498-01";
 export const runnerBasedEpic4dTestScenariosAmend = "@DTOSS-9497-01|@DTOSS-9499-01|@DTOSS-A451-01|@DTOSS-A452-01|@DTOSS-A453-01|@DTOSS-A454-01|@DTOSS-A455-01|@DTOSS-A456-01|@DTOSS-A457-01|@DTOSS-A458-01|@DTOSS-A459-01";
