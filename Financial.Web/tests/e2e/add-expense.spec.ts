import type { Page } from '@playwright/test'
import { API_PATH, expect, test } from './fixtures'

test.describe.configure({ mode: 'serial' })

async function openNewExpenseForm(page: Page) {
  await page.goto('/cashflow/monthly')
  await page.getByRole('tab', { name: 'Bank expenses' }).click()
  await page.getByRole('button', { name: 'New Expense' }).click()
}

async function fillValidExpense(page: Page, description: string) {
  await page.getByLabel('Description').fill(description)
  await page.getByLabel('Value').fill('12.34')
  await page.getByRole('combobox', { name: /^Category/ }).selectOption({ label: 'Mercado' })
  await page.getByRole('combobox', { name: /^Payment Source/ }).selectOption({ label: 'Barclays' })
}

test('@smoke an added expense appears in the monthly list', async ({ page, runId }) => {
  const description = `e2e-${runId}`
  await openNewExpenseForm(page)

  await fillValidExpense(page, description)
  await page.getByRole('button', { name: 'Add Expense' }).click()

  await expect(page.getByRole('cell', { name: description })).toBeVisible()
})

test('@smoke a blank value is rejected with a validation message and nothing is sent', async ({ page }) => {
  const posts: string[] = []
  page.on('request', (request) => {
    if (request.method() === 'POST' && request.url().includes(`${API_PATH}/expenses`)) posts.push(request.url())
  })
  await openNewExpenseForm(page)

  await page.getByLabel('Description').fill('blank value')
  await page.getByRole('button', { name: 'Add Expense' }).click()

  await expect(page.getByText('Value must be a non-zero number')).toBeVisible()
  await expect(page.getByRole('button', { name: 'Add Expense' })).toBeVisible()
  expect(posts).toEqual([])
})

test('@smoke a server error is shown and the form can be submitted again', async ({ page, runId, allowedConsoleErrors }) => {
  allowedConsoleErrors.push(/status of 500/)
  await page.route(`**${API_PATH}/expenses`, (route) =>
    route.request().method() === 'POST'
      ? route.fulfill({ status: 500, contentType: 'application/json', body: JSON.stringify({ title: 'Boom', detail: 'forced failure' }) })
      : route.continue(),
  )
  await openNewExpenseForm(page)

  await fillValidExpense(page, `e2e-fail-${runId}`)
  await page.getByRole('button', { name: 'Add Expense' }).click()

  await expect(page.getByText('forced failure')).toBeVisible()
  await expect(page.getByRole('button', { name: 'Add Expense' })).toBeEnabled()
})
