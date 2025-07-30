// For Future Epic 1 High Regression Tests

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
// - If custom calls are needed, use new file for test addition. - tests\playwright-tests\src\tests\e2e\epic1-highpriority-tests\epic1-high-priority-testsuite.spec.ts
//
// Test Tags:
// There is no impact to the test tags. The same tags can be used to trigger these tests.
// For example, to run regression tests, use:
//
//   npm run test:regression_e2e_epic1
//
// This equates to "@epic1-" tags, configured in the package.json at the playwright-tests root location.


export const runnerBasedEpic1TestScenariosAdd = "@DTOSS-3648-01|@DTOSS-3661-01|@DTOSS-3662-01|@DTOSS-3197-01|@DTOSS-3744-01|@DTOSS-3660-01";
export const runnerBasedEpic1TestScenariosAmend = "@DTOSS-3217-01|@DTOSS-3661-02|@DTOSS-3662-02|@DTOSS-3217-02";

