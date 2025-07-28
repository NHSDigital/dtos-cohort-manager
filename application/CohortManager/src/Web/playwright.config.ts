import { defineConfig, devices } from "@playwright/test";
import { defineBddConfig } from "playwright-bdd";
/**
 * Read environment variables from file.
 * https://github.com/motdotla/dotenv
 */
import dotenv from "dotenv";
import path from "path";

dotenv.config({ path: path.resolve(__dirname, ".env.tests") });

/**
* Define the BDD config.
*/
const testDir = defineBddConfig({
  features: "./tests/features/*.feature",
  steps: "./tests/features/steps/*.ts",
});

/**
 * See https://playwright.dev/docs/test-configuration.
 */
export default defineConfig({
  testDir,
  /* Run tests in files in parallel */
  fullyParallel: true,
  /* Fail the build on CI if you accidentally left test.only in the source code. */
  forbidOnly: !!process.env.CI,
  /* Retry on CI only */
  retries: process.env.CI ? 2 : 0,
  /* Opt out of parallel tests on CI. */
  workers: process.env.CI ? 1 : undefined,
  /* Reporter to use. See https://playwright.dev/docs/test-reporters */
  reporter: "html",
  /* Shared settings for all the projects below. See https://playwright.dev/docs/api/class-testoptions. */
  use: {
    baseURL: "https://localhost:3000",

    /* Collect trace when retrying the failed test. See https://playwright.dev/docs/trace-viewer */
    trace: "on",
    screenshot: "on",
    viewport: { width: 1280, height: 720 },
    video: "on",
  },

  /* Configure projects for major browsers */
  projects: [
    {
      name: "chromium",
      use: { ...devices["Desktop Chrome"] },
    },

    {
      name: "firefox",
      use: { ...devices["Desktop Firefox"] },
    },
    {
      name: "webkit",
      use: { ...devices["Desktop Safari"] },
    },
    // Test against mobile viewports.
    // Windows Browsers
    {
      name: 'Edge (Windows)',
      use: {
        channel: 'msedge',
      },
    },
    {
      name: 'Chrome (Windows)',
      use: {
        channel: 'chrome',
      },
    },
    {
      name: 'Firefox (Windows)',
      use: {
        browserName: 'firefox',
      },
    },

    // macOS Browsers
    {
      name: 'Safari (macOS)',
      use: {
        browserName: 'webkit',
      },
    },
    {
      name: 'Chrome (macOS)',
      use: {
        channel: 'chrome',
      },
    },
    {
      name: 'Firefox (macOS)',
      use: {
        browserName: 'firefox',
      },
    },

    // iOS Safari (emulated)
    {
      name: 'Safari (iOS)',
      use: {
        ...devices['iPhone 13'],
      },
    },
    {
      name: 'Chrome (iOS)',
      use: {
        ...devices['iPhone 13'],
        browserName: 'webkit', // Still uses WebKit on iOS
      },
    },

    // Android Browsers (emulated)
    {
      name: 'Chrome (Android)',
      use: {
        ...devices['Pixel 5'],
        browserName: 'chromium',
      },
    },
    {
      name: 'Edge (Android)',
      use: {
        ...devices['Pixel 5'],
        browserName: 'chromium', // Playwright can't simulate Edge exactly, just Chromium
      },
    },
    {
      name: 'Firefox (Android)',
      use: {
        ...devices['Pixel 5'],
        browserName: 'firefox',
        isMobile: undefined,
        deviceScaleFactor: undefined,
        hasTouch: undefined,
      },
    },
  ],
});
