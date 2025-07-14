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
//   npm run test:regression:e2e:epic4d
//
// This equates to "@epic4d-" tags, configured in the package.json at the playwright-tests root location.


 export const runnerBasedEpic4dTestScenariosAdd = "@DTOSS-8923-01";
 export const runnerBasedEpic4dTestScenariosAmend = "@DTOSS-8923-01";
