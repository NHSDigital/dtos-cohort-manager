import { expect, Locator, Page } from "@playwright/test";
import BasePage from "./basePage";

export class CookiesPage extends BasePage {
  readonly page: Page;
  readonly emailId: Locator;

  constructor(page: Page) {
    super(page)
    this.page = page;
    this.emailId = page.locator('[id="email"]');
  }



  async verifyHeading() {
    //await this.waitForElementVisible(this.headingBreastScreening)
    //await this.waitForElementVisible(this.headingBreastScreening)
  }


}
