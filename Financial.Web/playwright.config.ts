import { defineConfig } from '@playwright/test'

const isCi = !!process.env.CI

export default defineConfig({
  testDir: './tests/e2e',
  globalSetup: './tests/e2e/global-setup.ts',
  forbidOnly: isCi,
  retries: isCi ? 2 : 0,
  workers: isCi ? 2 : undefined,
  reporter: isCi ? [['list'], ['html', { open: 'never' }]] : [['list']],
  use: {
    baseURL: process.env.SMOKE_APP_URL,
    headless: true,
    locale: 'en-GB',
    timezoneId: 'Europe/London',
    trace: 'on-first-retry',
    screenshot: 'only-on-failure',
    video: 'retain-on-failure',
  },
})
