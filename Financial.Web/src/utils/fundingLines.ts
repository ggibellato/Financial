import { formatCurrency, parseValidatedNumber, positiveAmount, toPence } from './formatters'

export const MAX_FUNDING_LINES = 5

export interface FundingLineValues {
  key: string
  bankId: string
  amount: string
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

export function incomeDepositAmount(netValue: number): number {
  const netInTenThousandths = Math.round(netValue * 10000)
  return Math.round((netInTenThousandths * 9) / 1000) / 100
}

export function fundingLineErrors(lines: readonly FundingLineValues[]): Partial<Record<string, string>> {
  const errors: Partial<Record<string, string>> = {}
  for (const line of lines) {
    if (!line.bankId) errors[fundingLineErrorKey(line, 'bankId')] = 'Choose a bank.'
    if (positiveAmount(line.amount) === null) errors[fundingLineErrorKey(line, 'amount')] = 'Amount must be greater than zero.'
  }
  return errors
}

export function fundingLineErrorKey(line: Pick<FundingLineValues, 'key'>, field: 'bankId' | 'amount'): string {
  return `line:${line.key}:${field}`
}
