import { expect, test } from './fixtures'

test('@smoke [AC P60-F13-consolidated-navigation-and-retirement-07] a Settlement account is settled on its account screen', async ({ page }) => {
  await page.goto('/cashflow/accounts')
  await page.getByRole('link', { name: 'Mãe', exact: true }).click()

  await page.getByRole('button', { name: 'Settle', exact: true }).click()
  await page.getByRole('button', { name: 'Save settlement', exact: true }).click()
  const confirmation = page.getByRole('dialog')
  await expect(confirmation).toContainText('Both totals become zero.')
  await confirmation.getByRole('button', { name: 'Save settlement', exact: true }).click()
  await expect(page.getByText('Settled', { exact: true })).toBeVisible()

  const settlementRow = page.getByRole('row').filter({ has: page.getByRole('cell', { name: 'Settlement', exact: true }) })
  await settlementRow.getByRole('button', { name: 'Delete entry' }).click()
  await page.getByRole('dialog').getByRole('button', { name: 'Delete', exact: true }).click()
  await expect(settlementRow).toHaveCount(0)
  await expect(page.getByText('Settled', { exact: true })).toHaveCount(0)
})
