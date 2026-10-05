import { API_PATH, expect, test } from './fixtures'

const SEED_YEAR = new Date().getFullYear() - 2
const SEED_MONTHLY_VALUE = 100
const EXPECTED_MERCADO_AVERAGE = '25.00'
const BARCLAYS_BANK_ID = '8f3b1c1a-2e3a-4b1a-9a7f-100000000001'
const MERCADO_CATEGORY_ID = '8f3b1c1a-2e3a-4b1a-9a7f-600000000008'

test('@smoke the Historic Summary Average shows the value computed from seeded expenses', async ({ page, request }, testInfo) => {
  const months = testInfo.retry === 0 ? ['01', '02', '03'] : []
  for (const month of months) {
    const response = await request.post(`${API_PATH}/expenses`, {
      data: {
        date: `${SEED_YEAR}-${month}-05`,
        description: 'Smoke test seed',
        value: SEED_MONTHLY_VALUE,
        categoryId: MERCADO_CATEGORY_ID,
        paymentSourceBankId: BARCLAYS_BANK_ID,
        creditCardId: null,
      },
    })
    expect(response.ok(), `seeding ${SEED_YEAR}-${month}`).toBe(true)
  }

  await page.goto('/cashflow/annual-summary')
  await page.getByRole('tab', { name: 'Historic Summary Average' }).click()

  const mercadoRow = page.getByRole('row').filter({ has: page.getByRole('cell', { name: 'Mercado', exact: true }) })
  await expect(mercadoRow).toContainText(EXPECTED_MERCADO_AVERAGE)
})
