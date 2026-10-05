import { expect, test } from './fixtures'

test('@smoke the app loads and the investment tree renders', async ({ page }) => {
  await page.goto('/')

  await expect(page.getByText('XPI').first()).toBeVisible()
})
