import { defineConfig } from '@playwright/test';
import { config } from './env';

export default defineConfig({
  testDir: '../tests',
  timeout: 30 * 60 * 1000,    // 15 minutes
  retries: 2,
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
