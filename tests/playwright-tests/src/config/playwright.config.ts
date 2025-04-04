import { defineConfig } from '@playwright/test';
import { config } from './env';

export default defineConfig({
  testDir: '../tests',
  timeout: 3 * 60 * 1000,    // 3 minutes or 180000 milliseconds
  retries: 2,
  workers: 4,
  use: {
    baseURL: config.baseURL,
    extraHTTPHeaders: { 'Content-Type': 'application/json' },
  },
  projects: [
    { name: 'dev', use: { ...config } }
  ],
  reporter: [['html', { outputFolder: 'playwright-report' }]],
});
