import { expect, test } from './fixtures'

const STATEMENT = 'October 2026'

test('@smoke [AC P60-F13-consolidated-navigation-and-retirement-07] a card statement is paid from a bank on the card screen', async ({ page }) => {
  await page.goto('/cashflow/accounts')
  await page.getByRole('link', { name: 'Amex', exact: true }).click()
  await page.getByRole('tab', { name: 'Statements' }).click()

  await page.getByRole('button', { name: `Pay ${STATEMENT} statement` }).click()
  await page.getByRole('combobox', { name: /^Bank/ }).selectOption({ label: 'Barclays' })
  await page.getByRole('button', { name: 'Pay statement', exact: true }).click()

  const statementRow = page.getByRole('row').filter({ has: page.getByRole('cell', { name: STATEMENT, exact: true }) })
  await expect(statementRow).toContainText('from Barclays')

  await statementRow.getByRole('button', { name: `Unmark ${STATEMENT} statement` }).click()
  await page.getByRole('dialog').getByRole('button', { name: 'Unmark', exact: true }).click()
  await expect(statementRow.getByRole('button', { name: `Pay ${STATEMENT} statement` })).toBeVisible()
})
