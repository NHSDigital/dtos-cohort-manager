// For Future Epic 4c Test

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
//   npm run test:regression_e2e_epic4c
//
// This equates to "@epic4c-" tags, configured in the package.json at the playwright-tests root location.

// Epic-4c Epic : "https://nhsd-jira.digital.nhs.uk/browse/DTOSS-6041" has been tested as part of 3082 Epic 3 which are already has test coverage and track the migration of Epic 3 High Priority tests
// The Epic 4c will be used to track the migration of all Epic 4c

export const runnerBasedEpic4cTestScenariosAdd = "@DTOSS-9337-01";
export const runnerBasedEpic4cTestScenariosAmend = "@DTOSS-9337-01";
// Include DTOSS-10022 manual self-referral validation alongside existing scenario
export const runnerBasedEpic4cTestScenariosManualAdd = "@DTOSS-3883-01|@DTOSS-8569-01";
