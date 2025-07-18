import { test, expect } from "@playwright/test";

test("navigate to the Homepage as an unauthenticated user", async ({
  page,
}) => {
  await page.goto("/");
  await expect(
    page.getByRole("heading", {
      name: "Cohort Manager",
    })
  ).toBeVisible();
});

test("navigate to the Homepage and sign in as an authenticated user", async ({
  page,
}) => {
  await page.goto("/");
  await page.getByTestId("email").fill("test@nhs.net");
  await page.getByTestId("password").fill("password");
  await page.getByTestId("sign-in").click();
  await expect(
    page.getByRole("heading", {
      name: "Overview",
    })
  ).toBeVisible();
});
