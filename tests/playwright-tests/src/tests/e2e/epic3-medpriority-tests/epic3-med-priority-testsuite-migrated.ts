// For Future Epic 3 Med Regression Tests

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
// - If custom calls are needed, use new file for test addition. - tests\playwright-tests\src\tests\e2e\epic3-medpriority-tests\epic3-med-priority-testsuite.spec.ts
//
// Test Tags:
// There is no impact to the test tags. The same tags can be used to trigger these tests.
// For example, to run regression tests, use:
//
//   npm run test:regression_e2e_epic3Med
//
// This equates to "@epic3-med" tags, configured in the package.json at the playwright-tests root location.


export const runnerBasedEpic3MedTestScenariosAdd = "@DTOSS-4967-01|@DTOSS-4975-01|@DTOSS-6325-01|@DTOSS-5374-01|@DTOSS-6320-01|@DTOSS-5578-01|@DTOSS-5582-01|@DTOSS-9337-01"

export const runnerBasedEpic3MedTestScenariosAmend = "@DTOSS-5286-01|@DTOSS-5579-01|@DTOSS-5799-01|@DTOSS-5800-01|@DTOSS-5583-01|@DTOSS-9337-01"
