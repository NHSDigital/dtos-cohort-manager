import { defineConfig } from '@playwright/test';
import { config } from './env';

export default defineConfig({
  testDir: '../tests',
  timeout: 30 * 60 * 1000,    // 15 minutes
  expect: {
    timeout: 10000, // Sets the default assertion timeout to 10 seconds (10000ms)
  },
  retries: 1,
  workers: 1,
  fullyParallel: false,
  use: {
    baseURL: config.baseURL,
    extraHTTPHeaders: { 'Content-Type': 'application/json' },
  },
  projects: [
    { name: 'dev', use: { ...config } }
  ],
  reporter: [
    ['html', { outputFolder: 'playwright-report', open: 'never' }],
    ['junit', { outputFile: 'playwright-report/results.xml' }]
  ]
});
