import { formatCurrency, parseValidatedNumber } from './formatters'

export const MAX_FUNDING_LINES = 5

export interface FundingLineValues {
  key: string
  bankId: string
  amount: string
}

export function positiveAmount(value: string): number | null {
  const amount = parseValidatedNumber(value)
  return amount !== null && amount > 0 ? amount : null
}

export function toPence(value: number): number {
  return Math.round(value * 100)
}

function linesTotalInPence(lines: readonly FundingLineValues[]): number {
  return lines.reduce((total, line) => total + toPence(parseValidatedNumber(line.amount) ?? 0), 0)
}

export function fundingLinesMismatch(lines: readonly FundingLineValues[], depositAmount: number | null, currency: string): string | null {
  if (depositAmount === null) return null
  const total = linesTotalInPence(lines)
  if (total === toPence(depositAmount)) return null
  return `Lines add up to ${formatCurrency(total / 100, currency)}; the deposit is ${formatCurrency(depositAmount, currency)}.`
}

export function remainingAmount(lines: readonly FundingLineValues[], depositAmount: number | null): string {
  if (depositAmount === null) return ''
  const remaining = toPence(depositAmount) - linesTotalInPence(lines)
  return remaining > 0 ? (remaining / 100).toFixed(2) : ''
}

export function fundingLineErrorKey(line: Pick<FundingLineValues, 'key'>, field: 'bankId' | 'amount'): string {
  return `line:${line.key}:${field}`
}
