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
  // runs after each scenario
  await page.evaluate(() => window.scrollTo(0, 0));
  await page.waitForTimeout(1000);
});
