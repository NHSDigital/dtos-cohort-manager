import { defineConfig } from '@playwright/test';
import { config } from './env';

export default defineConfig({
  testDir: '../tests',
  timeout: 10 * 60 * 1000,    // 10 minutes
  retries: 2,
  workers: 4,
  fullyParallel: true,
  use: {
    baseURL: config.baseURL,
    extraHTTPHeaders: { 'Content-Type': 'application/json' },
  },
  projects: [
    { name: 'dev', use: { ...config } }
  ],
  reporter: [['html', { outputFolder: 'playwright-report' }]],
});
