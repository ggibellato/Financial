// Browser smoke test: loads the app against a running API and confirms
// the tree renders with no console errors. Run via `npm run smoke-test`
// after the API and web dev server are already up (see CI workflow).
import { chromium } from 'playwright'

const APP_URL = process.env.SMOKE_APP_URL ?? 'http://localhost:5173'
const TIMEOUT_MS = 15000

async function main() {
  const browser = await chromium.launch()
  const page = await browser.newPage()

  const consoleErrors = []
  page.on('console', (msg) => {
    if (msg.type() === 'error') consoleErrors.push(msg.text())
  })
  page.on('pageerror', (err) => consoleErrors.push(String(err)))

  await page.goto(APP_URL, { waitUntil: 'domcontentloaded' })

  // Broker name from Tests/Financial.Api.Tests/TestData/data.test.json
  await page.getByText('XPI').first().waitFor({ timeout: TIMEOUT_MS })

  if (consoleErrors.length > 0) {
    console.error('Smoke test failed: console errors detected')
    for (const err of consoleErrors) console.error(` - ${err}`)
    await browser.close()
    process.exit(1)
  }

  console.log('Smoke test passed: app loaded and rendered the navigation tree with no console errors')
  await browser.close()
}

main().catch((err) => {
  console.error('Smoke test failed:', err)
  process.exit(1)
})
