import { expect, Locator, Page } from "@playwright/test";
import BasePage from "./basePage";


export class ExceptionInformationPage extends BasePage {
  readonly page!: Page;
  readonly participantDetails: Locator;
  readonly exceptionDetails: Locator;
  readonly enterServiceNowCaseID: Locator;
  readonly saveandContinue: Locator;
  readonly notRaisedLink: Locator;
  readonly headingParticipantDetails: Locator;
  readonly headingExceptionDetails: Locator;

  constructor(page: Page) {
    super(page)
    this.page = page;
    this.participantDetails = page.locator('[data-testid="participant-details-section"]');
    this.exceptionDetails = page.locator('[data-testid="exception-details-section"]');
    this.enterServiceNowCaseID = page.locator('[data-testid="service-now-case-id-label"]');
    this.saveandContinue = page.locator('[data-testid="save-continue-button"]');
    this.notRaisedLink = page.getByRole('link', { name: 'Not raised breast screening exceptions', exact: true });
    this.headingParticipantDetails = page.getByRole('heading', { name: 'Participant details' });
    this.headingExceptionDetails = page.getByRole('heading', { name: 'Exception details' });
  }
  async getParticipantDetailsFields(): Promise<string[]> {
    await this.headingParticipantDetails.scrollIntoViewIfNeeded();
    return this.participantDetails.locator('dt.nhsuk-summary-list__key').allTextContents();

  }
  async getExceptionDetailsFields(): Promise<string[]> {
    await this.headingExceptionDetails.scrollIntoViewIfNeeded();
    return this.exceptionDetails.locator('dt.nhsuk-summary-list__key').allTextContents();

  }
  async clickOnNotRaisedLink() {
    await this.clickElement(this.notRaisedLink);
  }
  async getExceptionStatusText(): Promise<string> {
    await this.enterServiceNowCaseID.scrollIntoViewIfNeeded();
    return (await this.enterServiceNowCaseID.textContent())?.trim() ?? '';
  }
  async isSaveAndContinueButtonVisible(): Promise<boolean> {
    return await this.saveandContinue.isVisible();
  }

}
