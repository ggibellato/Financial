import type { Page } from '@playwright/test'
import { expect, test } from './fixtures'

const DEPOSIT = 100
const POTS_TOTAL = /^Pots total GBP:/

async function potsTotal(page: Page): Promise<number> {
  const text = await page.getByText(POTS_TOTAL).textContent()
  return Number(text!.replace(/[^\d.-]/g, ''))
}

test('@smoke [AC P60-F13-consolidated-navigation-and-retirement-07] a manual deposit with no bank reaches the Pots tab', async ({ page, runId }) => {
  const description = `e2e-${runId}`
  await page.goto('/cashflow/accounts')
  await page.getByRole('link', { name: 'Reserva', exact: true }).click()
  await expect(page.getByText(POTS_TOTAL)).toBeVisible()
  const totalBefore = await potsTotal(page)

  await page.getByRole('button', { name: 'New deposit', exact: true }).click()
  await page.getByRole('checkbox', { name: /^No bank/ }).check()
  await page.getByLabel(/^Amount/).fill(String(DEPOSIT))
  await page.getByLabel(/^Description/).fill(description)
  await page.getByRole('button', { name: 'Add deposit', exact: true }).click()

  await expect.poll(() => potsTotal(page)).toBe(totalBefore + DEPOSIT)
  await page.getByRole('tab', { name: 'Transactions' }).click()
  const depositRows = page.getByRole('row').filter({ hasText: description })
  await expect(depositRows.first()).toBeVisible()

  await depositRows.first().getByRole('button', { name: new RegExp(`^Delete deposit ${description}, `) }).click()
  await page.getByRole('dialog').getByRole('button', { name: 'Delete', exact: true }).click()
  await expect(depositRows).toHaveCount(0)
})
