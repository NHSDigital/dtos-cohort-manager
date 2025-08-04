import { expect, Locator, Page } from "@playwright/test";
import BasePage from "./basePage";


export class HomePage extends BasePage {
  readonly page!: Page;
  readonly emailId: Locator;
  readonly password: Locator;
  readonly signInButton: Locator;
  readonly headingBreastScreening: Locator;
  readonly headingExceptions: Locator;
  readonly raisedLink: Locator;
  readonly raisedCard: Locator;
  readonly notRaisedLink: Locator;
  readonly notRaisedCard: Locator;
  readonly reportLink: Locator;
  readonly reportCard: Locator;
  readonly headingRaised: Locator;
  readonly headingNotRaised: Locator;
  readonly headingReport: Locator;
  readonly contactUsLink: Locator;
  readonly teamsAndconditionsLink;
  readonly cookiesLink;

  constructor(page: Page) {
    super(page)
    this.emailId = page.locator('[data-testid="email"]');
    this.password = page.locator('[data-testid="password"]');
    this.signInButton = page.locator('[data-testid="sign-in"]');
    this.headingBreastScreening = page.getByRole('heading', { name: 'Breast screening' });
    this.headingExceptions = page.getByRole('heading', { name: 'Exceptions' });
    this.raisedLink = page.getByRole('link', { name: 'Raised', exact: true });
    this.raisedCard = page.locator('[data-testid="card"]', { hasText: "Raised" })
    this.notRaisedLink = page.getByRole('link', { name: 'Not raised', exact: true });
    this.notRaisedCard = page.locator('[data-testid="card"]', { hasText: "Not raised" });
    this.reportLink = page.getByRole('link', { name: 'Reports', exact: true });
    this.reportCard = page.locator('[data-testid="card"]', { hasText: "Reports" });;
    this.headingRaised = page.locator('[data-testid="heading-raised"]')
    this.headingNotRaised = page.locator('[data-testid="heading-not-raised"]')
    this.headingReport = page.locator('[data-testid="heading-report"]')
    this.contactUsLink = page.locator('[data-testid="contact-us-link"]')
    this.teamsAndconditionsLink = page.locator('[data-testid="terms-and-conditions-link"]')
    this.cookiesLink = page.locator('[data-testid="cookies-link"]')

  }

  async signInwithCredentials(emailId: string, password: string) {
    await this.clickElement(this.emailId)
    await this.fillFormField(this.emailId, emailId)
    await this.clickElement(this.password)
    await this.fillFormField(this.password, password)
    await this.clickElement(this.signInButton)
  }

  async verifyHeading() {
    await this.waitForElementVisible(this.headingBreastScreening)
    await this.waitForElementVisible(this.headingBreastScreening)
  }
  //Raised card
  async verifyRaised() {
    await this.waitForElementVisible(this.raisedLink)

  }
  async assertRaisedCardNumberIsAtLeast(min: number = 1) {
    const raisedNumber = await this.getCardNumber(this.raisedCard);
    const numberText = await raisedNumber.nth(1).textContent()
    console.log(numberText);
    const number = parseInt((numberText ?? '0').trim(), 10);
    expect(number).toBeGreaterThanOrEqual(min);
  }
  async verifyRaisedText(text: string) {
    const raisedcardtext = await this.getCardDescription(this.raisedCard);
    const raisedText = await raisedcardtext.nth(1).textContent();
    console.log(raisedText)
    expect(raisedText).toContain(text)
  }
  //Not Raised card
  async verifyNotRaised() {
    await this.waitForElementVisible(this.notRaisedLink)
  }
  async assertNotRaisedCardNumberIsAtLeast(min: number = 0) {
    const notRaisedCardNumber = await this.getCardNumber(this.notRaisedCard);
    const numberText = await notRaisedCardNumber.nth(0).textContent();
    console.log(numberText);
    const number = parseInt((numberText ?? '0').trim(), 10);
    if (number === 0) {
      await expect(this.notRaisedLink).toBeDisabled();
    }
    expect(number).toBeGreaterThanOrEqual(min);
  }
  async verifyNotRaisedText(text: string) {
    const notraisedcardtext = await this.getCardDescription(this.notRaisedCard);
    const notraisedText = await notraisedcardtext.nth(0).textContent();
    console.log(notraisedText)
    expect(notraisedText).toContain(text)
  }
  //Report card
  async verifyReportLink() {
    await this.waitForElementVisible(this.reportLink)
  }
  async assertReportCardNumberIsAtLeast(min: number = 0) {
    const reportCardNumber = await this.getCardNumber(this.reportCard);
    const numberText = await reportCardNumber.nth(0).textContent()
    console.log(numberText);
    const number = parseInt((numberText ?? '0').trim(), 10);
    expect(number).toBeGreaterThanOrEqual(min);
  }
  async verifyReportText(text: string) {
    const reportCardText = await this.getCardDescription(this.reportCard);
    const reportText = await reportCardText.nth(0).textContent();
    console.log(reportText)
    expect(reportText).toContain(text)
  }
  //Raised page
  async clickOnRaised() {
    await this.clickElement(this.raisedLink)
  }
  async verifyTextOnRaisedscreen(text: string) {
    await expect(this.headingRaised).toHaveText(text)
  }
  //Not Raised page
  async clickOnNotRaised() {
    await this.clickElement(this.notRaisedLink)
  }
  async verifyTextOnNotRaisedscreen(text: string) {
    await expect(this.headingNotRaised).toHaveText(text)
  }
  //Report page
  async clickOnReport() {
    await this.clickElement(this.reportLink)
  }
  async verifyTextOnReportscreen(text: string) {
    await expect(this.headingReport).toHaveText(text)
  }
  //contact us link
  async clickOnContactUs() {
    await this.clickElement(this.contactUsLink)
  }
  //terms and conditions link link
  async clicktermsAndconditionsLink() {
    await this.clickElement(this.teamsAndconditionsLink)
  }
  //cookies  link
  async clickOnCookiesLink() {
    await this.clickElement(this.cookiesLink)
  }
}
