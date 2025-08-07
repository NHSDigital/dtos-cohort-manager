import { expect, Locator, Page } from "@playwright/test";
import BasePage from "./basePage";
export class ExceptionOverviewPage extends BasePage {
  readonly page!: Page;
  readonly exceptionTable: Locator;
  readonly exceptiontableHeaders: Locator;
  readonly homepageLink: Locator;
  readonly exceptionIDLink: Locator;
  readonly sortByDateExceptionCreated: Locator;
  readonly applyButton: Locator;

  constructor(page: Page) {
    super(page)
    this.page = page;
    this.exceptionTable = page.locator('[data-testid="exceptions-table"]');
    this.exceptiontableHeaders = page.locator('[data-testid="exceptions-table"] th');
    this.homepageLink = page.getByRole('link', { name: 'Home', exact: true });
    this.exceptionIDLink = page.locator('[data-testid="exceptions-table"] tbody tr:first-child td:nth-child(1) a');
    //this.exceptionIDLink = page.locator('[data-testid="exceptions-table"] tbody tr:nth-child(2) td:nth-child(1) a');
    this.sortByDateExceptionCreated = page.locator('[data-testid="sort-not-raised-exceptions"]');
    this.applyButton = page.locator('[data-testid="apply-button"]');
  }
  async getTableHeaders(): Promise<string[]> {
    return this.page.$$eval('[data-testid="exceptions-table"] th', headers =>
      headers.map(h => h.textContent?.trim() || '')
    );
  }
  async clickOnHome() {
    await this.clickElement(this.homepageLink)
  }
  async verifySortnotavailable() {
    const headers = this.exceptiontableHeaders
    const count = await headers.count();
    for (let i = 0; i < count; i++) {
      // Check for aria-sort attribute
      const ariaSort = await headers.nth(i).getAttribute('aria-sort');
      expect(ariaSort).toBeNull();
      // Check for sort icon (example: using a class or data-testid)
      const sortIcon = await headers.nth(i).locator('.sort-icon, [data-testid="sort-icon"]').count();
      expect(sortIcon).toBe(0);
      // Optionally, check for clickable/sortable role
      const role = await headers.nth(i).getAttribute('role');
      expect(role).not.toBe('button');
    }
  }
  async clickOnexceptionID() {
    await this.clickElement(this.exceptionIDLink)
  }
  async sortByDateExceptionCreatedDescending(optionText: string) {
    // Adjust selector to match your sortable column header
    await this.sortByDateExceptionCreated.selectOption({ label: optionText });
    await this.applyButton.click();
  }
  async getDateExceptionCreatedColumn(): Promise<Date[]> {
    // Adjust selector to match the correct column index for "Date exception created"
    const dateCells = await this.page.locator('[data-testid="exceptions-table"] tbody tr td:nth-child(3)').allTextContents();
    return dateCells.map(text => new Date(text.trim()));
  }

}
