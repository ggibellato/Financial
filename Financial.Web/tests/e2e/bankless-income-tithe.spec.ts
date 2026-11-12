import type { Page } from '@playwright/test'
import { expect, test } from './fixtures'

const NET_VALUE = 100
const TITHE_RATE = 0.1

async function titheFigure(page: Page, label: string): Promise<number> {
  const text = await page.getByRole('group', { name: 'Tithe for October 2026' }).textContent()
  const match = new RegExp(`${label}: (-?[\\d,]+\\.\\d{2})`).exec(text ?? '')
  return match ? Number(match[1].replace(/,/g, '')) : Number.NaN
}

test('@smoke [AC P61-F03-tithe-tab-and-history-07] a bankless income raises the Tithe tab income base and tithe', async ({ page, runId }) => {
  await page.goto('/cashflow/accounts?month=2026-10&tab=tithe')
  await expect(page.getByRole('group', { name: 'Tithe for October 2026' })).toBeVisible()
  const baseBefore = await titheFigure(page, 'Income base')
  const titheBefore = await titheFigure(page, 'Tithe')

  await page.getByRole('tab', { name: 'Income' }).click()
  await page.getByRole('button', { name: 'New income', exact: true }).click()
  await page.getByLabel(/^Date/).fill('2026-10-20')
  await page.getByLabel(/^Source/).selectOption({ label: 'DividendoJuros' })
  await page.getByLabel(/^Bank/).selectOption({ label: '— No bank —' })
  await page.getByLabel(/^Description/).fill(`e2e-${runId}`)
  await page.getByLabel(/^Net value/).fill(String(NET_VALUE))
  await page.getByRole('button', { name: 'Add income', exact: true }).click()
  await expect(page.getByRole('row').filter({ hasText: `e2e-${runId}` }).first()).toBeVisible()

  await page.getByRole('tab', { name: 'Tithe' }).click()
  await expect.poll(() => titheFigure(page, 'Income base')).toBeCloseTo(baseBefore + NET_VALUE, 2)
  expect(await titheFigure(page, 'Tithe')).toBeCloseTo(titheBefore + NET_VALUE * TITHE_RATE, 2)
  await expect(page.getByRole('table', { name: 'Tithe by month, 2026' }).getByRole('row').filter({ hasText: 'October 2026' })).toHaveAttribute('aria-current', 'date')
})
