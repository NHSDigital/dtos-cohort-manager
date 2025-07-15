// For Future Epic 2 Med Regression Tests

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
// - If custom calls are needed, use new file for test addition. - tests\playwright-tests\src\tests\e2e\epic2-medpriority-tests\epic2-med-priority-testsuite.spec.ts
//
// Test Tags:
// There is no impact to the test tags. The same tags can be used to trigger these tests.
// For example, to run epic 2 med regression tests, use:
//
//   npm run test:regression_e2e_epic2Med
//
// This equates to "@epic2-med" tags, configured in the package.json at the playwright-tests root location.
//

export const runnerBasedEpic2MedTestScenariosAdd = "@DTOSS-5399-01|@DTOSS-5210-01|@DTOSS-5111-01|@DTOSS-4156-01|@DTOSS-4159-01|@DTOSS-4168-01";

export const runnerBasedEpic2MedTestScenariosAmend = "@DTOSS-4585-01";


