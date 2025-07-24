import { expect } from '@playwright/test';
import { Given, When, Then } from './fixtures';
import { HomePage } from '../pages/homePage';
import { ContactusPage } from '../pages/contactUsPage';
import { CookiesPage } from '../pages/cookiePage';
import { TermsConditionsPage } from '../pages/termsAndconditionsPage';  // Importing the TermsConditionsPage class

let homePage: HomePage;
let contactusPage: ContactusPage;
let cookiesPage: CookiesPage;
let termsConditionsPage: TermsConditionsPage; // Initializing the TermsConditionsPage class

Then('they should navigate to Terms and conditions page', async ({ page }) => {

  termsConditionsPage = new TermsConditionsPage(page)
  await expect(page).toHaveTitle('Terms and conditions - Cohort Manager - NHS');

});

Then('they should navigate to cookies page', async ({ page }) => {

  cookiesPage = new CookiesPage(page)
  await expect(page).toHaveTitle('Cookies on Cohort Manager - Cohort Manager - NHS');

});
Given('the User navigate to terms and conditions page', async ({ page }) => {
  homePage = new HomePage(page)
  termsConditionsPage = new TermsConditionsPage(page)
  await page.goto("/");
  await homePage.clicktermsAndconditionsLink()
  await expect(page).toHaveTitle('Terms and conditions - Cohort Manager - NHS');

});
When('the user clicks on Care Identity Service link', async ({ }) => {
  await termsConditionsPage.clickCISLink()
});

Then('they should navigate to Care Identity Service - NHS England Digital page', async ({ page }) => {
  await expect(page).toHaveTitle('Care Identity Service - NHS England Digital');
});

When('the user clicks on CIS and NHS Spine terms and conditions link', async ({ }) => {
  await termsConditionsPage.clickCISandNHSLink()
});

Then('they should navigate to terms and conditions for Care Identity Service and NHS Spine users page', async ({ page }) => {
  await expect(page).toHaveTitle('Privacy notice and terms and conditions for Care Identity Service and NHS Spine users - NHS England Digital');
});

When('the user clicks on cookies policy link', async ({ }) => {
  await termsConditionsPage.clickCookiesPolicyLink()
});
