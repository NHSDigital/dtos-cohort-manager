import { expect,Locator,Page } from "@playwright/test";
import BasePage from "./basePage";


export class HomePage extends BasePage{
     readonly page!: Page;
     readonly emailId:Locator;
     readonly password:Locator;
     readonly signInButton:Locator;
     readonly headingBreastScreening: Locator;
     readonly headingExceptions: Locator;
     readonly raisedLink: Locator;
     readonly raisedCardNumber: Locator;
     readonly raisedText:Locator;
     readonly notRaisedLink: Locator;
     readonly notRaisedCardNumber: Locator;
     readonly notRaisedText:Locator;
     readonly reportLink: Locator;
     readonly reportCardNumber: Locator;
     readonly reportText:Locator;
     readonly headingRaised:Locator;
     readonly headingNotRaised:Locator;
     readonly headingReport:Locator;
     readonly contactUsLink:Locator;
     readonly teamsAndconditionsLink;
     readonly cookiesLink;

    constructor(page:Page){
        super(page)
        this.emailId = page.locator('[id="email"]');
        this.password = page.locator('[id="password"]');
        this.signInButton = page.locator('[data-testid="sign-in"]');
        this.headingBreastScreening = page.getByRole('heading', { name: 'Breast screening' });
        this.headingExceptions = page.getByRole('heading', { name: 'Exceptions' });
        this.raisedLink = page.getByRole('link', { name: 'Raised', exact: true });
        this.raisedCardNumber = page.locator('//a[text()="Raised"]/ancestor::div[contains(@class, "nhsuk-card__content")]/p[contains(@class, "nhsuk-heading-xl")]');
        this.raisedText = page.locator('//a[text()="Raised"]/ancestor::div[contains(@class, "nhsuk-card__content")]/p[contains(@class, "nhsuk-card__description")]');
        this.notRaisedLink = page.getByRole('link', { name: 'Not raised', exact: true });
        this.notRaisedCardNumber = page.locator('//a[text()="Not raised"]/ancestor::div[contains(@class, "nhsuk-card__content")]/p[contains(@class, "nhsuk-heading-xl")]');
        this.notRaisedText = page.locator('//a[text()="Not raised"]/ancestor::div[contains(@class, "nhsuk-card__content")]/p[contains(@class, "nhsuk-card__description")]');
        this.reportLink = page.getByRole('link', { name: 'Reports', exact: true });
        this.reportCardNumber = page.locator('//a[text()="Reports"]/ancestor::div[contains(@class, "nhsuk-card__content")]/p[contains(@class, "nhsuk-heading-xl")]');
        this.reportText = page.locator('//a[text()="Reports"]/ancestor::div[contains(@class, "nhsuk-card__content")]/p[contains(@class, "nhsuk-card__description")]');
        this.headingRaised = page.locator('//div[@class="nhsuk-grid-column-full"]//h1[1]')
        this.headingNotRaised = page.locator('//div[@class="nhsuk-grid-column-full"]//h1[1]')
        this.headingReport = page.locator('//div[@class="nhsuk-grid-column-two-thirds"]//h1[1]')
        this.contactUsLink = page.locator('//a[normalize-space(text())="Contact us"]')
        this.teamsAndconditionsLink = page.locator('//a[normalize-space(text())="Terms and conditions"]')
        this.cookiesLink = page.locator('//a[normalize-space(text())="Cookies"]')

    }

    async signInwithCredentials(emailId:string,password:string){
        //await this.emailId.click();
        await this.clickElement(this.emailId)
        // await this.emailId.fill(emailId);
        await this.fillFormField(this.emailId,emailId)
        //await this.password.click();
        await this.clickElement(this.password)
        //await this.password.fill(password)
         await this.fillFormField(this.password,password)
         await this.clickElement(this.signInButton)

    }

    async verifyHeading(){
        await this.waitForElementVisible(this.headingBreastScreening)
        await this.waitForElementVisible(this.headingBreastScreening)
    }
 //Raised card
    async verifyRaised(){
        await this.waitForElementVisible(this.raisedLink)
        await this.waitForElementVisible(this.headingBreastScreening)
    }
    async assertRaisedCardNumberIsAtLeast(min: number = 1) {
     await this.raisedCardNumber.waitFor({ state: 'visible', timeout: 10000 });
     const numberText = await this.raisedCardNumber.textContent();
     console.log(numberText);
     const number = parseInt((numberText ?? '0').trim(), 10);
     expect(number).toBeGreaterThanOrEqual(min);
    }
    async verifyRaisedText(text:string){
     await expect(this.raisedText).toHaveText(text)
    }
 //Not Raised card
    async verifyNotRaised(){
     await this.waitForElementVisible(this.notRaisedLink)
    }
    async assertNotRaisedCardNumberIsAtLeast(min: number = 0) {
     await this.notRaisedCardNumber.waitFor({ state: 'visible', timeout: 10000 });
     const numberText = await this.notRaisedCardNumber.textContent();
     console.log(numberText);
     const number = parseInt((numberText ?? '0').trim(), 10);
     expect(number).toBeGreaterThanOrEqual(min);
    }
    async verifyNotRaisedText(text:string){
     await expect(this.notRaisedText).toHaveText(text)
    }
//Report card
    async verifyReportLink(){
     await this.waitForElementVisible(this.reportLink)
    }
    async assertReportCardNumberIsAtLeast(min: number = 0) {
     await this.reportCardNumber.waitFor({ state: 'visible', timeout: 10000 });
     const numberText = await this.reportCardNumber.textContent();
     console.log(numberText);
     const number = parseInt((numberText ?? '0').trim(), 10);
     expect(number).toBeGreaterThanOrEqual(min);
    }
    async verifyReportText(text:string){
     await this.reportText.waitFor({ state: 'visible', timeout: 10000 });
     await expect(this.reportText).toHaveText(text)
    }
 //Raised page
    async clickOnRaised(){
     await this.clickElement(this.raisedLink)
    }
    async verifyTextOnRaisedscreen(text:string){
     await expect(this.headingRaised).toHaveText(text)
    }
//Not Raised page
    async clickOnNotRaised(){
     await this.clickElement(this.notRaisedLink)
    }
    async verifyTextOnNotRaisedscreen(text:string){
     await expect(this.headingNotRaised).toHaveText(text)
    }
 //Report page
    async clickOnReport(){
     await this.clickElement(this.reportLink)
    }
    async verifyTextOnReportscreen(text:string){
     await expect(this.headingReport).toHaveText(text)
    }
 //contact us link
    async clickOnContactUs(){
     await this.clickElement(this.contactUsLink)
    }
//terms and conditions link link
    async clicktermsAndconditionsLink(){
     await this.clickElement(this.teamsAndconditionsLink)
    }
 //cookies  link
    async clickOnCookiesLink(){
     await this.clickElement(this.cookiesLink)
    }

}
