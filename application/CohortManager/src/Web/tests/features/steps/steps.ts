import { createBdd } from "playwright-bdd";
import { test } from "playwright-bdd";

const { When, Then } = createBdd(test);

When("I go to the page {string}", async ({ page }, path: string) => {
  await page.goto(path);
});

Then("I see the link {string}", async ({ page }, linkText: string) => {
  const link = page.getByRole("link", { name: linkText });
  await test.expect(link).toBeVisible();
});

Then(
  "the link {string} should have href {string}",
  async ({ page }, linkText: string, expectedHref: string) => {
    const link = page.getByRole("link", { name: linkText });
    await test.expect(link).toHaveAttribute("href", expectedHref);
  }
);
