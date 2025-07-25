import { expect } from '@playwright/test';
import { Given, When, Then } from './fixtures';
import { HomePage } from '../pages/homePage';
import { ContactusPage } from '../pages/contactUsPage';

let homePage: HomePage;
let contactUsPage: ContactusPage;

Then('they should navigate to contact us page and have title {string}', async ({ page }, title) => {
  contactUsPage = new ContactusPage(page)
  await expect(page).toHaveTitle(title);
});

Given('the User navigate to contact us page', async ({ page }) => {
  homePage = new HomePage(page)
  contactUsPage = new ContactusPage(page)
  await page.goto("/");
  await homePage.clickOnContactUs()
  await expect(page).toHaveTitle('Get help with Cohort Manager - Cohort Manager - NHS');

});
When('the user clicks on technical support and general enquiries link', async ({ page }) => {
  await contactUsPage.clickTechnicalLink()
});

Then('they should navigate to NHS National IT Customer Support Portal page', async ({ page }) => {
  await expect(page).toHaveTitle('Customer Service Portal - Customer Support');
});
When('the user clicks on Report an incident link', async ({ page }) => {
  await contactUsPage.clickRapidincidentLink()
});
