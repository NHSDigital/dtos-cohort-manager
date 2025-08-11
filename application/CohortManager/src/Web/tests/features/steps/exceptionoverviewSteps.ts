import { expect } from '@playwright/test';
import { Given, When, Then } from './fixtures';
import { HomePage } from '../pages/homePage';
import { ExceptionOverviewPage } from '../pages/exceptionOverviewPage';

let homePage: HomePage;
let exceptionOverviewPage: ExceptionOverviewPage;
Given('the user navigate to raised exception overview page', async ({ page }) => {
  homePage = new HomePage(page)
  await page.goto("/");
  await homePage.signInwithCredentials('test@test.com', 'test123')
  await homePage.clickOnRaised()
  await page.waitForTimeout(3000);

});

Then('the exception summary table should have the following columns:', async ({ page }, table: any) => {
  exceptionOverviewPage = new ExceptionOverviewPage(page);
  // Convert DataTable to array
  const rows: string[][] = typeof table.raw === 'function' ? table.raw() : table;
  const expectedHeaders = rows.map(row => row[0]);
  const actualHeaders = await exceptionOverviewPage.getTableHeaders();
  expect(actualHeaders).toEqual(expectedHeaders);
});

When('the user clicks on Home link', async ({ page }) => {
  exceptionOverviewPage = new ExceptionOverviewPage(page);
  await exceptionOverviewPage.clickOnHome()
});
Then('the exception summary table headers should not be sortable', async ({ page }) => {
  exceptionOverviewPage = new ExceptionOverviewPage(page);
  await exceptionOverviewPage.verifySortnotavailable();

});
When('the user clicks on exception ID link', async ({ page }) => {
  exceptionOverviewPage = new ExceptionOverviewPage(page);
  await exceptionOverviewPage.clickOnexceptionID()
});
Given('the user navigate to not raised exception overview page', async ({ page }) => {
  homePage = new HomePage(page)
  await page.goto("/");
  await homePage.signInwithCredentials('test@test.com', 'test123')
  await homePage.clickOnNotRaised()
  await page.waitForTimeout(3000);

});
When('the user sorts the exception summary table by {string}', async ({ page }, sortOption: string) => {
  const exceptionOverviewPage = new ExceptionOverviewPage(page);
  await exceptionOverviewPage.sortOptionSelect(sortOption);
  await page.waitForTimeout(3000);
});
Then('the exception summary table should be sorted by Date exception created in descending order', async ({ page }) => {
  const exceptionOverviewPage = new ExceptionOverviewPage(page);
  const dates = await exceptionOverviewPage.getDateExceptionCreatedColumn();
  const sorted = [...dates].sort((a, b) => b.getTime() - a.getTime());
  expect(dates).toEqual(sorted);
});
Then('the exception summary table should be sorted by Date exception created in ascending order', async ({ page }) => {
  const exceptionOverviewPage = new ExceptionOverviewPage(page);
  const dates = await exceptionOverviewPage.getDateExceptionCreatedColumn();
  const sorted = [...dates].sort((a, b) => a.getTime() - b.getTime());
  expect(dates).toEqual(sorted);
});
Then('the exceptions should be sorted by exception status last updated in descending order', async ({ page }) => {
  const exceptionOverviewPage = new ExceptionOverviewPage(page);
  const dates = await exceptionOverviewPage.getStatusUpdateDates();
  const isDescending = dates.every((d, i, arr) => i === 0 || arr[i - 1].getTime() >= d.getTime());
  expect(isDescending).toBe(true);
});
Then('the exceptions should be sorted by exception status last updated in ascending order', async ({ page }) => {
  const exceptionOverviewPage = new ExceptionOverviewPage(page);
  const dates = await exceptionOverviewPage.getStatusUpdateDates();
  const isAscending = dates.every((d, i, arr) => i === 0 || arr[i - 1].getTime() <= d.getTime());
  expect(isAscending).toBe(true);
});
// Use a WeakMap to store sort errors per context
const sortErrorMap = new WeakMap();
When('the user sorts with unsupported options by {string}', async ({ page }, sortOption: string) => {
  const exceptionOverviewPage = new ExceptionOverviewPage(page);
  try {
    await Promise.race([
      exceptionOverviewPage.sortOptionSelect(sortOption),
      new Promise((_, reject) => setTimeout(() => reject(new Error('Custom timeout')), 3000))
    ]);
    sortErrorMap.set(page.context(), null);
  } catch (error) {
    sortErrorMap.set(page.context(), error);
  }
});
Then('the error message should thrown', async ({ page }) => {
  // Retrieve the error from the WeakMap for this context
  const error = sortErrorMap.get(page.context());
  expect(error).toBeDefined();
  expect(
    error.message.includes('did not find some options') ||
    error.message.includes('Custom timeout')
  ).toBe(true);
});
