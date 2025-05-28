// These tests have now been fully migrated to the new and improved
// test execution orchestration under tests/runner/runner-workflow-add for bulk addition
// and tests/runner/runner-workflow-amend for add followed by amend.
// This allows bulk loading of test data for all tests and then proceeding with validation,
// as opposed to loading test data for each test one at a time.

// Note: First try adding new tests using the runner. If any custom calls are needed,
// then use this file for test addition.


// There is no impact to the test tags. The same tags can be used to trigger these tests.
// For example, if you need to run smoke tests, use `npm test` which equates to @smoke @e2e;
// these are configured inside the package.json file at the root location.


// Examples when you might need a custom call & assertions
// tests\playwright-tests\src\tests\api\bsSelect\smoke\bs-select-endpoints.spec.ts
// tests\playwright-tests\src\tests\api\bsSelect\regression\bs-select-api-retrieve-cohort-data.spec.ts
// tests\playwright-tests\src\tests\api\bsSelect\deleteFunction\delete-participant-function.spec.ts
