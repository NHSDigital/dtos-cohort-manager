import { expect } from '@playwright/test';
import { Given, When, Then } from './fixtures';
import { HomePage } from '../pages/homePage';
import { ExceptionOverviewPage } from '../pages/exceptionOverviewPage';
import { ExceptionInformationPage } from '../pages/exceptionInformationPage';

let homePage: HomePage;
let exceptionOverviewPage: ExceptionOverviewPage;
let exceptionInformationPage: ExceptionInformationPage;

Then('the participant details section should have the following fields:', async ({ page }, table: any) => {
  const exceptionInformationPage = new ExceptionInformationPage(page);
  const actualLabels = await exceptionInformationPage.getParticipantDetailsFields();
  const expectedLabels = typeof table.raw === 'function'
    ? table.raw().map((row: string[]) => row[0])
    : table.map((row: string[]) => row[0]);
  expect(actualLabels).toEqual(expectedLabels);
});
Then('the Exception details section should have the following fields:', async ({ page }, table: any) => {
  const exceptionInformationPage = new ExceptionInformationPage(page);
  const actualLabels = await exceptionInformationPage.getExceptionDetailsFields();
  const expectedLabels = typeof table.raw === 'function'
    ? table.raw().map((row: string[]) => row[0])
    : table.map((row: string[]) => row[0]);
  expect(actualLabels).toEqual(expectedLabels);
});
Then('the following labels should be present on top of the page:', async ({ page }, table: any) => {
  const exceptionInformationPage = new ExceptionInformationPage(page);
  const actualLabels = await exceptionInformationPage.getExceptionDetailsLabels();
  const expectedLabels = typeof table.raw === 'function'
    ? table.raw().map((row: string[]) => row[0])
    : table.map((row: string[]) => row[0]);
  expect(actualLabels).toEqual(expectedLabels);
});

When('the user clicks on Not raised breast screening exceptions link', async ({ page }) => {
  const exceptionInformationPage = new ExceptionInformationPage(page);
  await exceptionInformationPage.clickOnNotRaisedLink();
});
When('the user clicks on raised breast screening exceptions link', async ({ page }) => {
  const exceptionInformationPage = new ExceptionInformationPage(page);
  await exceptionInformationPage.clickOnRaisedLink();
});
Then('the Exception status have {string}', async ({ page }, expectedText) => {
  const exceptionInformationPage = new ExceptionInformationPage(page);
  const actualText = await exceptionInformationPage.getExceptionStatusText();
  expect(actualText).toContain(expectedText);
});

Then('the Exception status have {string} button', async ({ page }, buttonText) => {
  const exceptionInformationPage = new ExceptionInformationPage(page);
  // Find the button by text within the Exception status section
  const button = exceptionInformationPage.saveandContinue;
  await expect(button).toBeVisible();
  const actualText = await button.textContent();
  expect(actualText?.toLowerCase()).toContain(buttonText.toLowerCase());
});

