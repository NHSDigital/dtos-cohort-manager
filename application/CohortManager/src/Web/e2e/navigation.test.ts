import { test, expect } from "@playwright/test";

test("navigate to the Homepage as an unauthenticated user", async ({
  page,
}) => {
  await page.goto("/");
  await expect(
    page.getByRole("heading", {
      name: "Log in with your Care Identity account",
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

// TODO: Log in with an account that does not have access to the Cohort Manager and expect to see the Unauthorised page

// TODO: Check the number of exceptions on the Overview page is displayed

// TODO: Navigate to the Exceptions summary page from the Overview page

// TODO: Try to access the Exceptions summary page without being logged in and expect to be redirected to the Log in with your Care Identity account page

// TODO: Navigate back from the from the Exceptions summary page to the Overview page

// TODO: Randomly select an exception ID from API data in the Exceptions summary page and navigate to the Participant details page

// TODO: Make sure that the expected content from the API is display on the Participant details page

// TODO: Try to access the Participant details page without being logged in and expect to be redirected to the Log in with your Care Identity account page

// TODO: Make sure you can navigate back from the from the articipant details page to the Exceptions summary page
