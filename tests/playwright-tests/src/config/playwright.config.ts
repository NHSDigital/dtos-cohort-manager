import { defineConfig } from '@playwright/test';
import { config } from './env';

export default defineConfig({
  testDir: '../tests',
  timeout: 60000,
  retries: 2,
  workers: 4,
  use: {
    baseURL: config.baseURL,
    extraHTTPHeaders: { 'Content-Type': 'application/json' },
  },
  projects: [
    { name: 'dev', use: { ...config } },
    { name: 'staging', use: { ...config } },
  ],
  reporter: [['html', { outputFolder: 'playwright-report' }]],
});
