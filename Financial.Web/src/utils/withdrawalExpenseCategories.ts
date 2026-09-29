import type { CategoryDto } from '../api/types'

export const RESERVA_CATEGORY_NAME = 'Reserva'

const sameName = (a: string, b: string) => a.toLowerCase() === b.toLowerCase()

export function eligibleWithdrawalCategories(categories: CategoryDto[]): CategoryDto[] {
  return categories.filter((c) => c.active && !c.isInvestment && !sameName(c.name, RESERVA_CATEGORY_NAME))
}

export function defaultExpenseCategoryId(eligibleCategories: CategoryDto[], bucketName: string): string {
  return eligibleCategories.find((c) => sameName(c.name, bucketName))?.id ?? ''
}
