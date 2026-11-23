import type { BankOption } from '../api/types'
import { hasAtMostTwoDecimals, parseValidatedNumber, positiveAmount } from './formatters'

export const CASHBACK_RANGE_MESSAGE = 'Cashback must be between 0 and the expense value, with at most 2 decimal places.'

const PENNIES_IN_A_POUND = 100
const FLOATING_POINT_DIGITS = 6

export function cashbackRateOf(banks: readonly BankOption[], bankId: string): number | null {
  const bank = banks.find((candidate) => candidate.id === bankId)
  return bank?.cashbackEnabled ? (bank.cashbackRatePercent ?? null) : null
}

export function suggestCashbackAmount(banks: readonly BankOption[], bankId: string, value: string): string | null {
  const rate = cashbackRateOf(banks, bankId)
  const parsedValue = positiveAmount(value)
  if (rate === null || parsedValue === null) return null
  const pennies = Math.floor(Number((parsedValue * rate).toFixed(FLOATING_POINT_DIGITS)))
  return (pennies / PENNIES_IN_A_POUND).toFixed(2)
}

export function parseCashbackAmount(cashback: string, expenseValue: number): number | null {
  const amount = parseValidatedNumber(cashback, { min: 0, max: expenseValue })
  return amount !== null && hasAtMostTwoDecimals(cashback) ? amount : null
}
