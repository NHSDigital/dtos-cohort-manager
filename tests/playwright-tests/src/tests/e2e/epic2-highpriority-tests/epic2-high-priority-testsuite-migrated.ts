// For Future Epic 2 High Regression Tests

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
// - If custom calls are needed, use new file for test addition. - tests\playwright-tests\src\tests\e2e\epic2-highpriority-tests\epic2-high-priority-testsuite.spec.ts
//
// Test Tags:
// There is no impact to the test tags. The same tags can be used to trigger these tests.
// For example, to run regression tests, use:
//
//   npm run test:regression_e2e_epic2
//
// This equates to "@epic2-" tags, configured in the package.json at the playwright-tests root location.
//

export const runnerBasedEpic2TestScenariosAdd = "@DTOSS-5104-01|@DTOSS-5613-01|@DTOSS-4395-01|@DTOSS-4397-01|@DTOSS-4562-01|@DTOSS-4563-01|@DTOSS-3206-01|@DTOSS-4089-01|@DTOSS-4102-01|@DTOSS-4088-01|@DTOSS-4103-01|@DTOSS-4092-01|@DTOSS-4099-01";

export const runnerBasedEpic2TestScenariosAmend = "@DTOSS-5605-01|@DTOSS-4396-01|@DTOSS-5419-01|@DTOSS-4068-01|@DTOSS-4070-01|@DTOSS-4564-01|@DTOSS-4091-01|@DTOSS-4561-01|@DTOSS-5418-01|@DTOSS-4090-01|@DTOSS-4384-01|@DTOSS-4094-01|@DTOSS-4219-01|@DTOSS-4383-01|@DTOSS-4220-01";
