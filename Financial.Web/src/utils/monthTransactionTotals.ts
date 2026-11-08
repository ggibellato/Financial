import type { MonthTransactionDto } from '../api/types'
import { toPence } from './formatters'

export interface CurrencyTotal {
  currency: string
  total: number
  spend: number
}

export function monthTransactionTotals(rows: MonthTransactionDto[]): CurrencyTotal[] {
  const penceByCurrency = new Map<string, number>()
  for (const row of rows) {
    const { amount, currency } = row.transaction
    penceByCurrency.set(currency, (penceByCurrency.get(currency) ?? 0) + toPence(amount))
  }
  return [...penceByCurrency.entries()]
    .sort(([a], [b]) => a.localeCompare(b))
    .map(([currency, pence]) => ({ currency, total: pence / 100, spend: -pence / 100 || 0 }))
}
