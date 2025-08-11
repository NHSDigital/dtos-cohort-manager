import { test as base, createBdd } from 'playwright-bdd';

type Fixtures = {
  // set types of your fixtures
};

export const test = base.extend<Fixtures>({
  // add your fixtures
});

export const { Given, When, Then } = createBdd(test);
const { AfterScenario } = createBdd(test);

AfterScenario(async ({ page }) => {
  // Scroll to bottom
  await page.evaluate(() => window.scrollTo(0, document.body.scrollHeight));
  await page.waitForTimeout(1000); // Wait for video to capture the bottom
  // runs after each scenario
  await page.evaluate(() => window.scrollTo(0, 0));
  await page.waitForTimeout(1000);
  // Take a screenshot after each scenario
  const timestamp = Date.now();
  await page.screenshot({ path: `screenshots/afterScenario_${timestamp}.png`, fullPage: true });
});
