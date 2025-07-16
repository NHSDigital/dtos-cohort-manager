import { expect } from '@playwright/test';
import { Given, When, Then } from './fixtures';
import { HomePage } from '../pages/homePage';
import { ContactusPage } from '../pages/contactUsPage';

let homePage: HomePage;
let contactUsPage: ContactusPage;

Then('they should navigate to contact us page', async ({ page }) => {
  contactUsPage = new ContactusPage(page)
  await expect(page).toHaveTitle('Get help with Cohort Manager - Cohort Manager');
});

Given('the User has login and navigate to contact us page', async ({ page }) => {
  homePage = new HomePage(page)
  contactUsPage = new ContactusPage(page)
  await page.goto("/");
  await homePage.signInwithCredentials('test@test.com', 'test123')
  await homePage.clickOnContactUs()
  await expect(page).toHaveTitle('Get help with Cohort Manager - Cohort Manager');

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
