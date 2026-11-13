import { formatCurrency, parseValidatedNumber, positiveAmount, sumPence, toPence } from './formatters'

export const MAX_FUNDING_LINES = 5

const NET_PRECISION = 10000
const DEPOSIT_TENTHS_OF_NET = 9
const PENCE_IN_A_POUND = 100

export interface FundingLineValues {
  key: string
  bankId: string
  amount: string
}

export type FundingLineChange = Partial<Pick<FundingLineValues, 'bankId' | 'amount'>>

let fundingLineCount = 0

export function newFundingLine(bankId = '', amount = ''): FundingLineValues {
  fundingLineCount += 1
  return { key: `line-${fundingLineCount}`, bankId, amount }
}

export function withChangedLine(lines: readonly FundingLineValues[], key: string, change: FundingLineChange): FundingLineValues[] {
  return lines.map((line) => (line.key === key ? { ...line, ...change } : line))
}

export function withAddedLine(lines: readonly FundingLineValues[], depositAmount: number | null): FundingLineValues[] {
  return lines.length >= MAX_FUNDING_LINES ? [...lines] : [...lines, newFundingLine('', remainingAmount(lines, depositAmount))]
}

export function withoutLine(lines: readonly FundingLineValues[], key: string): FundingLineValues[] {
  return lines.filter((line) => line.key !== key)
}

export function comparableLines(lines: readonly FundingLineValues[]): { bankId: string; amount: string }[] {
  return lines.map(({ bankId, amount }) => ({ bankId, amount }))
}

export function toFundingLineRequests(lines: readonly FundingLineValues[]): { bankId: string; amount: number }[] {
  return lines.map(({ bankId, amount }) => ({ bankId, amount: Number(amount) }))
}

function linesTotalInPence(lines: readonly FundingLineValues[]): number {
  return sumPence(lines, (line) => parseValidatedNumber(line.amount) ?? 0)
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
  const scaledNet = Math.round(netValue * NET_PRECISION)
  const depositInPence = (scaledNet * DEPOSIT_TENTHS_OF_NET * PENCE_IN_A_POUND) / (NET_PRECISION * 10)
  return Math.round(depositInPence) / PENCE_IN_A_POUND
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
