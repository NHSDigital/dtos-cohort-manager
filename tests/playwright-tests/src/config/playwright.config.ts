import { defineConfig } from '@playwright/test';
import { config } from './env';

export default defineConfig({
  testDir: '../tests',
  timeout: 15 * 60 * 1000,    // 15 minutes
  globalTimeout: 50 * 60 * 1000, // 50 minutes - Total max time allowed for all tests to complete to avoid loss of reports as pipeline runner is configured to timeout after 60 minutes
  retries: 0,
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
