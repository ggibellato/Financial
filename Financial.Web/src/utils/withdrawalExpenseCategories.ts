import type { AccountDto, CategoryDto } from '../api/types'

const sameName = (a: string, b: string) => a.toLowerCase() === b.toLowerCase()

export function accountCategoryIds(accounts: AccountDto[]): string[] {
  return accounts.flatMap((account) => (account.categoryId ? [account.categoryId] : []))
}

export function eligibleWithdrawalCategories(categories: CategoryDto[], excludedCategoryIds: string[]): CategoryDto[] {
  return categories.filter((c) => c.active && !c.isInvestment && !excludedCategoryIds.includes(c.id))
}

export function defaultExpenseCategoryId(eligibleCategories: CategoryDto[], bucketName: string): string {
  return eligibleCategories.find((c) => sameName(c.name, bucketName))?.id ?? ''
}
