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
    } catch {}
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

// Aliases required by notRaisedExceptions.feature
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
  // Scope to either the tag class or a container test id if you have one
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

// Flexible substring check (alias wording)
Then(
  "I see text containing {string} in the element {string}",
  async ({ page }, text: string, el: string) => {
    const locator = page.locator(
      `[data-testid="${el}"], #${el}, [id="${el}"], [name="${el}"]`
    );
    await test.expect(locator).toContainText(text);
  }
);

// Regex-compatible check – if the pattern is not a valid regex, fall back to literal match
Then(
  "I see text matching {string} in the element {string}",
  async ({ page }, pattern: string, el: string) => {
    const locator = page.locator(
      `[data-testid="${el}"], #${el}, [id="${el}"], [name="${el}"]`
    );
    let regex: RegExp;
    try {
      regex = new RegExp(pattern, "i");
    } catch {
      regex = new RegExp(escapeRegExp(pattern), "i");
    }
    await test.expect(locator).toContainText(regex);
  }
);

Then("I do not see the link {string}", async ({ page }, linkText: string) => {
  const link = page.getByRole("link", { name: linkText });
  await test.expect(link).toHaveCount(0);
});

Then("I see the {string} element", async ({ page }, el: string) => {
  const locator = page.locator(
    `[data-testid="${el}"], #${el}, [id="${el}"], [name="${el}"]`
  );
  await test.expect(locator).toBeVisible();
});

// Generic visibility for ONLY link or text
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

// Button visibility
Then("I see the button {string}", async ({ page }, buttonText: string) => {
  await test
    .expect(page.getByRole("button", { name: buttonText }))
    .toBeVisible();
});

// Link with href check
Then(
  "I see the link {string} with the href {string}",
  async ({ page }, linkText: string, expectedHref: string) => {
    const exactName = new RegExp(`^\\s*${escapeRegExp(linkText)}\\s*$`, "i");
    const link = page.getByRole("link", { name: exactName });
    await test.expect(link).toBeVisible();
    await test.expect(link).toHaveAttribute("href", expectedHref);
  }
);

// Numeric assertion inside element
Then(
  "I see the number {int} in the element {string}",
  async ({ page }, num: number, el: string) => {
    const locator = page.locator(
      `[data-testid="${el}"], #${el}, [id="${el}"], [name="${el}"]`
    );
    await test.expect(locator).toContainText(String(num));
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
  await page
    .getByRole("button", { name: new RegExp(`^\\s*${buttonText}\\s*$`, "i") })
    .click();
});

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
  "the table {string} has columns",
  async ({ page }, tableId: string, dataTable) => {
    const expected = dataTable.raw().flat();
    const headers = await tableLocator(page, tableId)
      .locator("thead th, thead [role='columnheader']")
      .allTextContents();
    const normalized = headers.map((h: string) =>
      h.replace(/\s+/g, " ").trim()
    );
    for (const col of expected) {
      await test.expect(normalized).toContain(col);
    }
  }
);

Then(
  "the first row in the table {string} has exception ID {string}",
  async ({ page }, tableId: string, id: string) => {
    const firstRow = tableLocator(page, tableId).locator("tbody tr").first();
    await test.expect(firstRow).toContainText(id);
  }
);

Then(
  "the table {string} contains exception ID {string}",
  async ({ page }, tableId: string, id: string) => {
    await test
      .expect(tableLocator(page, tableId).locator("tbody"))
      .toContainText(id);
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
  // Open/select in the dropdown
  const form = page.getByTestId("sort-exceptions-form");
  const select = form.locator("select");
  await test.expect(select).toBeVisible();

  await select.selectOption({ label: sortOption });

  // Click the apply button
  const applyButton = page.getByTestId("apply-button");
  await test.expect(applyButton).toBeVisible();
  await applyButton.click();
});

// ---- Accessibility ----
Then(
  "I should expect {int} accessibility issues",
  async ({ page }, expectedCount: number) => {
    await injectAxe(page);

    const selector = '[data-testid="test-account-form"]'; // ignored element
    const context = { exclude: [[selector]] };

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

// Table heading check
Then("I see the table heading {string}", async ({ page }, heading: string) => {
  const name = new RegExp(`^\\s*${escapeRegExp(heading)}\\s*$`, "i");
  await test.expect(page.getByRole("columnheader", { name })).toBeVisible();
});

// ServiceNow ID text check
Then(
  "the ServiceNow ID has the text {string}",
  async ({ page }, expected: string) => {
    const locator = page
      .getByText("ServiceNow ID", { exact: false })
      .locator("..");
    await test.expect(locator).toContainText(expected);
  }
);

// Text input presence
Then(
  "I see the text input with label {string}",
  async ({ page }, label: string) => {
    const input = page.getByLabel(label, { exact: true });
    await test.expect(input).toBeVisible();
  }
);

// Values in list check
Then(
  "I should see the values in the {string} list:",
  async ({ page }, listId: string, dataTable) => {
    const expected = dataTable.raw().flat();
    const locator = page.locator(`[data-testid="${listId}"], #${listId}`);
    for (const value of expected) {
      await test.expect(locator).toContainText(value);
    }
  }
);

// Negative heading check
Then(
  "I should not see the heading {string}",
  async ({ page }, headingText: string) => {
    const heading = page.getByRole("heading", { level: 1, name: headingText });
    await test.expect(heading).toHaveCount(0);
  }
);

// Negative text input presence
Then(
  "I should not see the text input with label {string}",
  async ({ page }, label: string) => {
    const input = page.getByLabel(label, { exact: true });
    await test.expect(input).toHaveCount(0);
  }
);

// Negative button presence
Then(
  "the button {string} should not be present",
  async ({ page }, buttonText: string) => {
    const button = page.getByRole("button", { name: buttonText });
    await test.expect(button).toHaveCount(0);
  }
);

// -------------------- Pagination --------------------
function paginationLocator(page: Page) {
  return page.getByTestId("pagination").first();
}

Then("I see the pagination", async ({ page }) => {
  await test.expect(paginationLocator(page)).toBeVisible();
});

Then("I do not see the pagination", async ({ page }) => {
  await test.expect(page.getByTestId("pagination")).toHaveCount(0);
});

Then(
  "I see the previous pagination link with href {string}",
  async ({ page }, href: string) => {
    const prev = paginationLocator(page).locator('a[rel="prev"]');
    await test.expect(prev).toBeVisible();
    await test.expect(prev).toHaveAttribute("href", href);
  }
);

Then("I do not see the previous pagination link", async ({ page }) => {
  await test
    .expect(paginationLocator(page).locator('a[rel="prev"]'))
    .toHaveCount(0);
});

Then(
  "I see the next pagination link with href {string}",
  async ({ page }, href: string) => {
    const next = paginationLocator(page).locator('a[rel="next"]');
    await test.expect(next).toBeVisible();
    await test.expect(next).toHaveAttribute("href", href);
  }
);

Then("I do not see the next pagination link", async ({ page }) => {
  await test
    .expect(paginationLocator(page).locator('a[rel="next"]'))
    .toHaveCount(0);
});

Then(
  "the pagination has current page {int}",
  async ({ page }, current: number) => {
    const currentLink = paginationLocator(page).locator(
      'a[aria-current="page"]'
    );
    await test.expect(currentLink).toBeVisible();
    await test.expect(currentLink).toHaveText(String(current));
    // also assert proper aria-label
    await test
      .expect(currentLink)
      .toHaveAttribute("aria-label", `Page ${current}`);
  }
);

Then(
  "I see the page {int} link in the pagination with href {string}",
  async ({ page }, num: number, href: string) => {
    const link = paginationLocator(page).getByRole("link", {
      name: new RegExp(`^\\s*Page\\s+${num}\\s*$`, "i"),
    });
    await test.expect(link).toBeVisible();
    await test.expect(link).toHaveAttribute("href", href);
  }
);

When(
  "I click the page {int} link in the pagination",
  async ({ page }, num: number) => {
    await paginationLocator(page)
      .getByRole("link", { name: new RegExp(`^\\s*Page\\s+${num}\\s*$`, "i") })
      .click();
  }
);

When("I click the next pagination link", async ({ page }) => {
  await paginationLocator(page).locator('a[rel="next"]').click();
});

When("I click the previous pagination link", async ({ page }) => {
  await paginationLocator(page).locator('a[rel="prev"]').click();
});

Then("the pagination shows an ellipsis", async ({ page }) => {
  await test
    .expect(paginationLocator(page).locator(".app-pagination__ellipsis"))
    .toBeVisible();
});

Then("the pagination has pages:", async ({ page }, dataTable) => {
  const expected = dataTable
    .raw()
    .flat()
    .map((s: string) => s.trim());
  const labels = await paginationLocator(page)
    .locator('a[aria-label^="Page "]')
    .allTextContents();
  const nums = labels.map((t) => t.trim());
  for (const n of expected) {
    await test.expect(nums).toContain(n);
  }
});
