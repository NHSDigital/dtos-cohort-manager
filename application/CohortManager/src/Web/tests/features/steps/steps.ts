import { createBdd, test } from "playwright-bdd";
import type { Page } from "@playwright/test";
import { injectAxe, getViolations } from "axe-playwright";

const { Given, When, Then } = createBdd(test);

function escapeRegExp(s: string) {
  return s.replace(/[.*+?^${}()|[\]\\]/g, "\\$&");
}

// ---- Auth ----
Given("I am not authenticated", async ({ page }) => {
  await page.context().clearCookies();
  await page.addInitScript(() => {
    try {
      window.localStorage.clear();
      window.sessionStorage.clear();
    } catch (e) {
      console.error("Error clearing localStorage/sessionStorage:", e);
    }
  });
});

Given("I am signed in as {string} with password {string}", async ({ page }) => {
  await page.goto("/");
  await page
    .getByRole("button", { name: "Log in with a test account" })
    .click();
  await test
    .expect(page.getByRole("heading", { level: 1, name: "Breast screening" }))
    .toBeVisible();
});

Given("I sign in with a test account", async ({ page }) => {
  await page.goto("/");
  await page
    .getByRole("button", { name: "Log in with a test account" })
    .click();
  await test
    .expect(page.getByRole("heading", { level: 1, name: "Breast screening" }))
    .toBeVisible();
});

// ---- Navigation ----
When("I go to the page {string}", async ({ page }, path: string) => {
  await page.goto(path);
});

When("I click the link {string}", async ({ page }, linkText: string) => {
  await page.getByRole("link", { name: linkText }).click();
});

When(
  /^I click the (first|second|third) "([^"]+)" link$/,
  async ({ page }, ordinal: string, linkText: string) => {
    const mapping: Record<string, number> = { first: 0, second: 1, third: 2 };
    const index = mapping[ordinal.toLowerCase()];
    if (index === undefined)
      throw new Error(`Unsupported ordinal "${ordinal}"`);
    await page.getByRole("link", { name: linkText }).nth(index).click();
  }
);

Then(
  "the pagination shows page {string} as current",
  async ({ page }, current: string) => {
    const currentLink = paginationLocator(page).locator(
      'a[aria-current="page"]'
    );
    await test.expect(currentLink).toBeVisible();
    await test.expect(currentLink).toHaveText(current);
  }
);

Then(
  "the {string} control is not present",
  async ({ page }, control: string) => {
    const sel =
      control.toLowerCase() === "previous" ? 'a[rel="prev"]' : 'a[rel="next"]';
    await test.expect(paginationLocator(page).locator(sel)).toHaveCount(0);
  }
);

Then("the {string} control is present", async ({ page }, control: string) => {
  const sel =
    control.toLowerCase() === "previous" ? 'a[rel="prev"]' : 'a[rel="next"]';
  await test.expect(paginationLocator(page).locator(sel)).toBeVisible();
});

When(
  "I click the page number {string} in the pagination",
  async ({ page }, num: string) => {
    await paginationLocator(page)
      .getByRole("link", { name: new RegExp(`^\\s*Page\\s+${num}\\s*$`, "i") })
      .click();
  }
);

// ---- Assertions ----
Then(
  "I should see the heading {string}",
  async ({ page }, headingText: string) => {
    await test
      .expect(page.getByRole("heading", { level: 1, name: headingText }))
      .toBeVisible();
  }
);

Then(
  "I should see the secondary heading {string}",
  async ({ page }, headingText: string) => {
    await test
      .expect(page.getByRole("heading", { level: 2, name: headingText }))
      .toBeVisible();
  }
);

Then(
  "I should not see the secondary heading {string}",
  async ({ page }, headingText: string) => {
    await test
      .expect(page.getByRole("heading", { level: 2, name: headingText }))
      .toHaveCount(0);
  }
);

Then("I see the tag {string}", async ({ page }, text: string) => {
  const exact = new RegExp(`^\\s*${escapeRegExp(text)}\\s*$`, "i");
  const tag = page
    .locator('[data-testid="exception-details-labels"] .nhsuk-tag, .nhsuk-tag')
    .filter({ hasText: exact })
    .first();

  await test.expect(tag).toBeVisible();
});

Then(
  "I see the text {string} in the element {string}",
  async ({ page }, text: string, el: string) => {
    const locator = page.locator(
      `[data-testid="${el}"], #${el}, [id="${el}"], [name="${el}"]`
    );
    await test.expect(locator).toContainText(text);
  }
);

Then(
  "I see text containing {string} in the element {string}",
  async ({ page }, text: string, el: string) => {
    const locator = page.locator(
      `[data-testid="${el}"], #${el}, [id="${el}"], [name="${el}"]`
    );
    await test.expect(locator).toContainText(text);
  }
);

Then(
  /^I see the (link|text) "([^"]+)"$/,
  async ({ page }, type: string, value: string) => {
    if (type === "text") {
      await test.expect(page.getByText(value)).toBeVisible();
    } else {
      const exactName = new RegExp(`^\\s*${escapeRegExp(value)}\\s*$`, "i");
      await test
        .expect(page.getByRole("link", { name: exactName }))
        .toBeVisible();
    }
  }
);

Then("I see the button {string}", async ({ page }, buttonText: string) => {
  await test
    .expect(page.getByRole("button", { name: buttonText }))
    .toBeVisible();
});

Then(
  "I see the link {string} with the href {string}",
  async ({ page }, linkText: string, expectedHref: string) => {
    const exactName = new RegExp(`^\\s*${escapeRegExp(linkText)}\\s*$`, "i");
    const link = page.getByRole("link", { name: exactName });
    await test.expect(link).toBeVisible();
    await test.expect(link).toHaveAttribute("href", expectedHref);
  }
);

Then(
  /^I see the number in the (first|second|third) card(?: \(([^)]+)\))? is greater than or equal to (\d+)$/,
  async ({ page }, ordinal: string, _label: string, min: string) => {
    const mapping: Record<string, number> = { first: 0, second: 1, third: 2 };
    const index = mapping[ordinal.toLowerCase()];
    const card = page.getByTestId("card").nth(index);
    const text = await card.textContent();
    const num = parseInt(text || "0", 10);
    await test.expect(num).toBeGreaterThanOrEqual(Number(min));
  }
);

// --- Forms ---
When(
  "I fill the input with label {string} with {string}",
  async ({ page }, label: string, value: string) => {
    const input = page.getByLabel(label, { exact: true });
    await input.fill(value);
  }
);

When("I click the button {string}", async ({ page }, buttonText: string) => {
  await page.getByRole("button", { name: buttonText }).click();
});

Then(
  "I should see the error summary with message {string}",
  async ({ page }, errorMessage: string) => {
    const errorSummary = page.locator(".nhsuk-error-summary__list");
    await test.expect(errorSummary).toContainText(errorMessage);
  }
);

Then(
  "I should see the inline error message {string}",
  async ({ page }, errorMessage: string) => {
    const inlineError = page.locator(".nhsuk-error-message");
    await test.expect(inlineError).toContainText(errorMessage);
  }
);

// --- Tables ---
function tableLocator(page: Page, idOrTestId: string) {
  return page
    .locator(
      `[data-testid="${idOrTestId}"], #${idOrTestId}, [id="${idOrTestId}"]`
    )
    .first();
}

Then(
  "the table {string} has {int} rows",
  async ({ page }, tableId: string, expected: number) => {
    const rows = tableLocator(page, tableId).locator("tbody tr");
    await test.expect(rows).toHaveCount(expected);
  }
);

Then(
  "every row in the table {string} has status {string}",
  async ({ page }, tableId: string, status: string) => {
    const rows = tableLocator(page, tableId).locator("tbody tr");
    const rowCount = await rows.count();
    for (let i = 0; i < rowCount; i++) {
      await test.expect(rows.nth(i)).toContainText(status);
    }
  }
);

Then(
  "the first row in the table {string} has exception ID {string}",
  async ({ page }, tableId: string, id: string) => {
    const firstRow = tableLocator(page, tableId).locator("tbody tr").first();
    const exceptionIdLink = firstRow.locator("td").first().locator("a");
    await test.expect(exceptionIdLink).toContainText(id);
  }
);

Then(
  "the table {string} does not contain exception ID {string}",
  async ({ page }, tableId: string, id: string) => {
    await test
      .expect(tableLocator(page, tableId).locator("tbody"))
      .not.toContainText(id);
  }
);

When("I sort the table by {string}", async ({ page }, sortOption: string) => {
  const form = page.getByTestId("sort-exceptions-form");
  const select = form.locator("select");
  await test.expect(select).toBeVisible();

  const optionLocator = select.locator('option', { hasText: sortOption }).first();
  const value = await optionLocator.getAttribute('value');
  test.expect(value).not.toBeNull();

  await select.selectOption({ label: sortOption });

  const applyButton = page.getByTestId("apply-button");
  await test.expect(applyButton).toBeVisible();
  await Promise.all([
    page.waitForURL(new RegExp(String.raw`\bsortBy=${value}\b`)),
    applyButton.click(),
  ]);

  // Ensure the table has re-rendered
  const firstLink = tableLocator(page, "exceptions-table").locator("tbody tr").first().locator("td").first().locator("a");
  await test.expect(firstLink).toBeVisible();
});

// ---- Accessibility ----
const AXE_EXCLUDED_SELECTORS = ['[data-testid="test-account-form"]'];

Then(
  "I should expect {int} accessibility issues",
  async ({ page }, expectedCount: number) => {
    await injectAxe(page);
    const context: { include?: string[][]; exclude?: string[][] } = {
      exclude: AXE_EXCLUDED_SELECTORS.map((s) => [s]),
    };
    const violations = await getViolations(page, context);
    await test.expect(violations).toHaveLength(expectedCount);

    if (violations.length) {
      console.log(
        "Axe violations:",
        violations.map((v) => ({
          id: v.id,
          impact: v.impact,
          description: v.description,
          nodes: v.nodes?.length,
        }))
      );
    }
  }
);

Then("I see the table heading {string}", async ({ page }, heading: string) => {
  const name = new RegExp(`^\\s*${escapeRegExp(heading)}\\s*$`, "i");
  await test.expect(page.getByRole("columnheader", { name })).toBeVisible();
});

Then(
  "I see the text input with label {string}",
  async ({ page }, label: string) => {
    const input = page.getByLabel(label, { exact: true });
    await test.expect(input).toBeVisible();
  }
);

Then(
  "I should not see the text input with label {string}",
  async ({ page }, label: string) => {
    const input = page.getByLabel(label, { exact: true });
    await test.expect(input).toHaveCount(0);
  }
);

Then(
  "the button {string} should not be present",
  async ({ page }, buttonText: string) => {
    const button = page.getByRole("button", { name: buttonText });
    await test.expect(button).toHaveCount(0);
  }
);

// ---- Summary lists ----
Then(
  "I see the row {string} in the summary list",
  async ({ page }, rowId: string) => {
    const row = page.locator(`[data-testid="${rowId}"]`).first();
    await test.expect(row).toBeVisible();
  }
);

Then(
  "I see the text {string} in the {string} row",
  async ({ page }, text: string, rowId: string) => {
    const row = page.locator(`[data-testid="${rowId}"]`).first();
    await test.expect(row).toContainText(text);
  }
);

// -------------------- Pagination --------------------
function paginationLocator(page: Page) {
  return page.getByTestId("pagination").first();
}
