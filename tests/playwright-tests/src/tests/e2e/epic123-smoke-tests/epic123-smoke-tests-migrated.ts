// For Future Smoke Tests

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
// - If custom calls are needed, use this file for test addition.
//
// Test Tags:
// There is no impact to the test tags. The same tags can be used to trigger these tests.
// For example, to run smoke tests, use:
//
//   npm run test:smoke_e2e
//
// This equates to @smoke @e2e tags, configured in the package.json at the playwright-tests root location.
//
// Examples of custom calls & assertions:
//
// - tests/playwright-tests/src/tests/api/bsSelect/smoke/bs-select-endpoints.spec.ts
// - tests/playwright-tests/src/tests/api/bsSelect/regression/bs-select-api-retrieve-cohort-data.spec.ts
// - tests/playwright-tests/src/tests/api/bsSelect/deleteFunction/delete-participant-function.spec.ts



export const runnerBasedEpic123TestScenariosAdd = "@DTOSS-6256-01|@DTOSS-7960-01";
export const runnerBasedEpic123TestScenariosAddAmend = "@DTOSS-6257-01|@DTOSS-6407-01";
