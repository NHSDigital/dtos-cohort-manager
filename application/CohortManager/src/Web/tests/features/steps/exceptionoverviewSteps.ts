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
