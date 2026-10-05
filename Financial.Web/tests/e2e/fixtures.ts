import { randomUUID } from 'node:crypto'
import { expect, test as base } from '@playwright/test'

export const API_PATH = '/api/v1/financial'

interface Fixtures {
  runId: string
  allowedConsoleErrors: RegExp[]
}

export const test = base.extend<Fixtures>({
  // eslint-disable-next-line no-empty-pattern -- Playwright requires destructuring in the first argument
  runId: async ({}, use) => {
    await use(process.env.GITHUB_RUN_ID ?? randomUUID().slice(0, 8))
  },
  // eslint-disable-next-line no-empty-pattern -- Playwright requires destructuring in the first argument
  allowedConsoleErrors: async ({}, use) => {
    await use([])
  },
  page: async ({ page, allowedConsoleErrors }, use) => {
    const consoleErrors: string[] = []
    page.on('console', (message) => {
      if (message.type() === 'error') consoleErrors.push(message.text())
    })
    page.on('pageerror', (error) => consoleErrors.push(String(error)))

    await use(page)

    expect(
      consoleErrors.filter((error) => !allowedConsoleErrors.some((allowed) => allowed.test(error))),
      'browser console errors',
    ).toEqual([])
  },
})

export { expect }
