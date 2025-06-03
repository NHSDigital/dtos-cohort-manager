// For Future Epic 3 High Regression Tests

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
// - If custom calls are needed, use new file for test addition. - tests\playwright-tests\src\tests\e2e\epic3-highpriority-tests\epic3-high-priority-testsuite.spec.ts
//
// Test Tags:
// There is no impact to the test tags. The same tags can be used to trigger these tests.
// For example, to run regression tests, use:
//
//   npm run test:regression:e2e:epic3
//
// This equates to "@epic3-" tags, configured in the package.json at the playwright-tests root location.


export const runnerBasedEpic3TestScenariosAdd = "@DTOSS-5539-01|@DTOSS-5348-01|@DTOSS-5223-01";
export const runnerBasedEpic3TestScenariosAmend = "@DTOSS-5801-01|@DTOSS-5589-01|@DTOSS-5407-01|@DTOSS-5406-01|@DTOSS-5405-01|@DTOSS-5565-01|@DTOSS-5222-01";
