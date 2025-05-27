import { test, expect } from '@playwright/test';

test('@dummy test should pass', async () => {
  expect(1 + 1).toBe(2); // Dummy assertion to ensure the test passes
});

test('@dummy test should fail', async () => {
  expect(1 + 1).toBe(3); // Dummy assertion to ensure the test fails
});

test('@dummy test should skip', async () => {
  test.skip(true, 'Skipping this test intentionally'); // Skipping the test
});
