import { defineConfig } from '@playwright/test';
import { config } from './env';

const now = new Date();
const pad = (n: number) => n.toString().padStart(2, '0');
const dateTimeNow = `${pad(now.getDate())}_${pad(now.getMonth() + 1)}_${now.getFullYear().toString().slice(-2)}_${pad(now.getHours())}_${pad(now.getSeconds())}`;

export default defineConfig({
  testDir: '../tests',
  timeout: 15 * 60 * 1000,    // 15 minutes
  globalTimeout: 50 * 60 * 1000, // 50 minutes - Total max time allowed for all tests to complete to avoid loss of reports as pipeline runner is configured to timeout after 60 minutes
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
    ['html', { outputFolder: `playwright-report/${dateTimeNow}`, open: 'never'}],
    ['junit', { outputFile: `playwright-report/results.xml` }]
  ]
});
