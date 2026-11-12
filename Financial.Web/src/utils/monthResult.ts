import { toPence } from './formatters'

export interface MonthResult {
  value: number
  label: 'Surplus' | 'Deficit' | 'Balanced'
}

export function monthResult(totalIncoming: number, categoryTotalsSum: number): MonthResult {
  const pence = toPence(totalIncoming) - toPence(categoryTotalsSum)
  const label = pence > 0 ? 'Surplus' : pence < 0 ? 'Deficit' : 'Balanced'
  return { value: pence / 100, label }
}
