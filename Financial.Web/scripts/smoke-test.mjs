// Browser smoke test: loads the app against a running API and confirms
// the tree renders with no console errors, then seeds real CashFlow data
// through the API and confirms a specific computed value renders correctly.
// The second part exists because a bundle that merely *compiles* can still
// render wrong/blank data if the frontend's hand-written API types drift
// from what the backend actually returns (field renames, shape changes) -
// tsc has no way to catch that on its own.
// Run via `npm run smoke-test` after the API and web dev server are already
// up (see CI workflow).
import { chromium } from 'playwright'

const APP_URL = process.env.SMOKE_APP_URL ?? 'http://localhost:5173'
const API_BASE_URL = `${APP_URL}/api/v1/financial`
const TIMEOUT_MS = 15000

// A fully completed past year, so the average always divides by 12 months
// regardless of what day this test happens to run on.
const SEED_YEAR = new Date().getFullYear() - 2
const SEED_MONTHLY_VALUE = 100
const EXPECTED_MERCADO_AVERAGE = '25.00' // (100 * 3) / 12 months

async function seedMercadoExpenses() {
  for (const month of ['01', '02', '03']) {
    const response = await fetch(`${API_BASE_URL}/expenses`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        date: `${SEED_YEAR}-${month}-05`,
        description: 'Smoke test seed',
        value: SEED_MONTHLY_VALUE,
        category: 'Mercado',
        paymentSource: 'Barclays',
        cardTag: null,
      }),
    })
    if (!response.ok) {
      throw new Error(`Failed to seed expense for ${SEED_YEAR}-${month}: ${response.status} ${await response.text()}`)
    }
  }
}

async function main() {
  await seedMercadoExpenses()

  const browser = await chromium.launch()
  const context = await browser.newContext({ locale: 'en-US' })
  const page = await context.newPage()

  const consoleErrors = []
  page.on('console', (msg) => {
    if (msg.type() === 'error') consoleErrors.push(msg.text())
  })
  page.on('pageerror', (err) => consoleErrors.push(String(err)))

  await page.goto(APP_URL, { waitUntil: 'domcontentloaded' })

  // Broker name from Tests/Financial.Api.Tests/TestData/data.test.json
  await page.getByText('XPI').first().waitFor({ timeout: TIMEOUT_MS })

  await page.goto(`${APP_URL}/cashflow/annual-summary`, { waitUntil: 'domcontentloaded' })
  await page.getByRole('button', { name: 'Historic Summary Average' }).click()

  const mercadoCell = page.getByRole('cell', { name: 'Mercado', exact: true })
  await mercadoCell.waitFor({ timeout: TIMEOUT_MS })
  const mercadoRow = page.locator('tr', { has: mercadoCell })
  const mercadoRowText = await mercadoRow.textContent()

  if (!mercadoRowText?.includes(EXPECTED_MERCADO_AVERAGE)) {
    console.error(
      `Smoke test failed: Historic Summary Average did not show Mercado = ${EXPECTED_MERCADO_AVERAGE} for ${SEED_YEAR} ` +
        `(seeded 100 in each of Jan/Feb/Mar, expected sum/12 months). Row text was: "${mercadoRowText}". This usually ` +
        'means the frontend and API have drifted on the response shape for this endpoint.',
    )
    await browser.close()
    process.exit(1)
  }

  if (consoleErrors.length > 0) {
    console.error('Smoke test failed: console errors detected')
    for (const err of consoleErrors) console.error(` - ${err}`)
    await browser.close()
    process.exit(1)
  }

  console.log('Smoke test passed: app loaded, rendered the navigation tree, and the Historic Summary Average showed the expected computed value')
  await browser.close()
}

main().catch((err) => {
  console.error('Smoke test failed:', err)
  process.exit(1)
})
