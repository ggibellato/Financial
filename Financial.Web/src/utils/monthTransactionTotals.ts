import type { MonthTransactionDto } from '../api/types'
import { toPence } from './formatters'

export interface CurrencyTotal {
  currency: string | null
  total: number
  spend: number
}

function currenciesThenNoBank(a: string | null, b: string | null): number {
  if (a === null || b === null) return Number(a === null) - Number(b === null)
  return a.localeCompare(b)
}

export function monthTransactionTotals(rows: MonthTransactionDto[]): CurrencyTotal[] {
  const penceByCurrency = new Map<string | null, number>()
  for (const row of rows) {
    const { amount, currency } = row.transaction
    penceByCurrency.set(currency, (penceByCurrency.get(currency) ?? 0) + toPence(amount))
  }
  return [...penceByCurrency.entries()]
    .sort(([a], [b]) => currenciesThenNoBank(a, b))
    .map(([currency, pence]) => ({ currency, total: pence / 100, spend: -pence / 100 || 0 }))
}
