import { expect, Locator, Page } from "@playwright/test";
import BasePage from "./basePage";


export class ContactusPage extends BasePage {
  readonly page: Page;
  readonly technicalsupportLink: Locator;
  readonly rapidincidentLink: Locator;

  constructor(page: Page) {
    super(page)
    this.page = page;
    this.technicalsupportLink = page.locator('[data-testid="technical-support-link"]');
    this.rapidincidentLink = page.locator('[data-testid="report-incident-link"]');
  }
  async clickTechnicalLink() {
    await this.clickElement(this.technicalsupportLink)
  }

  async clickRapidincidentLink() {
    await this.clickElement(this.rapidincidentLink)
  }
}
