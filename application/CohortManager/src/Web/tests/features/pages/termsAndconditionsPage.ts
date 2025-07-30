import { expect, Locator, Page } from "@playwright/test";
import BasePage from "./basePage";


export class TermsConditionsPage extends BasePage {
  readonly page: Page;
  readonly cisLink: Locator;
  readonly cisandnhstermsLink: Locator;
  readonly cookiesPolicyLink: Locator;

  constructor(page: Page) {
    super(page)
    this.page = page;
    this.cisLink = page.locator('[data-testid="CIS-link"]');
    this.cisandnhstermsLink = page.locator('[data-testid="cis-and-nhs-terms-link"]');
    this.cookiesPolicyLink = page.locator('[data-testid="cookies-policy-link"]');
  }
  async clickCISLink() {
    await this.clickElement(this.cisLink)
  }

  async clickCISandNHSLink() {
    await this.clickElement(this.cisandnhstermsLink)
  }

  async clickCookiesPolicyLink() {
    await this.clickElement(this.cookiesPolicyLink)
  }

}
