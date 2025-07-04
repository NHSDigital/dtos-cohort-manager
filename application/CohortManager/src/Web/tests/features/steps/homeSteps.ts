import { expect } from '@playwright/test';
import { Given, When, Then } from './fixtures';
import { HomePage } from '../pages/homePage';

let homePage:HomePage;
/* Given('the User has logged in to the Cohort manager exceptions UI', async ({}) => {
  // Step: Given the User has logged in to the Cohort manager exceptions UI
  // From: tests/features/home.feature:4:2
});

When('they land on the {string}', async ({}, arg) => {
  // Step: When they land on the 'Breast screening - Cohort Manager'
  // From: tests/features/home.feature:6:2
});

Then('they should see Raised as link on raised card', async ({}) => {
  // Step: Then they should see Raised as link on raised card
  // From: tests/features/home.feature:10:2
});

Then('the total number should be displayed on raised', async ({}) => {
  // Step: And the total number should be displayed on raised
  // From: tests/features/home.feature:11:2
});

Then('they should be able to view {string} text under the Raised card', async ({}, arg) => {
  // Step: And they should be able to view 'Access and amend previously raised exceptions' text under the Raised card
  // From: tests/features/home.feature:12:2
}); */

Given('the User has logged in to the Cohort manager exceptions UI', async({ page })=>{

      homePage = new HomePage(page)
      await page.goto("/");
      await homePage.signInwithCredentials('test@test.com','test123')

});

 When('they land on the {string}', async ({ page }, title)=>{
  await expect(page).toHaveTitle(title);
  await homePage.verifyHeading();
});
//Raised verification steps
Then('they should see Raised as link on raised card',async ({})=>{
  await homePage.verifyRaised()

});
Then('the total number should be displayed on raised',async ({})=>{
  await homePage.assertRaisedCardNumberIsAtLeast()

});
Then('they should be able to view {string} text under the Raised card',async ({}, text)=>{
  await homePage.verifyRaisedText(text)

});

//Not Raised verification steps
/*
Then('they should see Not Raised as link on not raised card',async ()=>{
  await homePage.verifyNotRaised()

});
Then('the total number should be displayed on Not raised',async ()=>{
  await homePage.assertNotRaisedCardNumberIsAtLeast()

});
Then('they should be able to view {string} text under Not Raised card',async (text)=>{
  await homePage.verifyNotRaisedText(text)

});
//Report card verification steps
Then('they should see Report as link on Report card',async ()=>{
  await homePage.verifyReportLink()

});
Then('the total number should be displayed on Report card',async ()=>{
  await homePage.assertReportCardNumberIsAtLeast()

});
Then('they should be able to view {string} text under Report card',async (text)=>{
  await homePage.verifyReportText(text)

});
//navigate to raised
When('the user clicks on Raised link', async ()=>{
  await homePage.clickOnRaised()
});

Then('they should navigate to {string}',async (title)=>{
  await expect(pageFixture.page).toHaveTitle(title);
});
Then('they should see {string} on raised exception screen',async (text)=>{
  await homePage.verifyTextOnRaisedscreen(text)
});
//navigate to Not raised
When('the user clicks on Not Raised link', async ()=>{
  await homePage.clickOnNotRaised()
});

Then('they should directed to {string}',async (title)=>{
  await expect(pageFixture.page).toHaveTitle(title);
});
Then('they should see {string} on not raised exception screen',async (text)=>{
  await homePage.verifyTextOnNotRaisedscreen(text)
});
//navigate to Report
When('the user clicks on Report link', async ()=>{
  await homePage.clickOnReport()
});

Then('they should lands on {string}',async (title)=>{
  await expect(pageFixture.page).toHaveTitle(title);
});
Then('they should see {string} on Report screen',async (text)=>{
  await homePage.verifyTextOnReportscreen(text)
});
//navigate to contact Us
When('the user clicks on contact us link', async ()=>{
   homePage = new HomePage(pageFixture.page)
  await homePage.clickOnContactUs()
});
//navigate to terms and conditions
When('the user clicks on Terms and conditions link', async ()=>{
   homePage = new HomePage(pageFixture.page)
  await homePage.clicktermsAndconditionsLink()
});
//navigate to cookies page
When('the user clicks on cookies link', async ()=>{
   homePage = new HomePage(pageFixture.page)
  await homePage.clickOnCookiesLink()
});


 */
