import type { Page } from '@playwright/test'
import { expect, test } from './fixtures'

async function openNewExpenseForm(page: Page) {
  await page.goto('/cashflow/accounts')
  await page.getByRole('link', { name: 'Barclays' }).click()
  await page.getByRole('button', { name: 'New expense', exact: true }).click()
}

async function fillValidExpense(page: Page, description: string) {
  await page.getByLabel(/^Description/).fill(description)
  await page.getByLabel('Value').fill('12.34')
  await page.getByRole('combobox', { name: /^Category/ }).selectOption({ label: 'Mercado' })
}

test('@smoke an added expense appears on the account screen', async ({ page, runId }) => {
  const description = `e2e-${runId}`
  await openNewExpenseForm(page)

  await fillValidExpense(page, description)
  await page.getByRole('form', { name: 'New expense' }).getByRole('button', { name: 'Add expense', exact: true }).click()

  await expect(page.getByRole('cell', { name: description, exact: true })).toBeVisible()
})
