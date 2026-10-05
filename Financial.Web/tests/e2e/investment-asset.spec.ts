import { expect, test } from './fixtures'

test('@smoke an investment asset opens with its summary values', async ({ page }) => {
  await page.goto('/')

  await page.getByRole('treeitem', { name: /XPI/ }).click()
  await page.getByRole('treeitem', { name: /Default/ }).click()
  await page.getByRole('treeitem', { name: /BCIA11/ }).click()

  await expect(page.getByText('Total Bought (forced failure check)')).toBeVisible()
  await expect(page.getByText('TESTISIN')).toBeVisible()
})
