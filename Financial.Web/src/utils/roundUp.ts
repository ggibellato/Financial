import type { BankOption } from '../api/types'
import { parseValidatedNumber, toPence } from './formatters'

const MIN_ROUND_UP_AMOUNT = 0
const MAX_ROUND_UP_AMOUNT = 0.99

export const ROUND_UP_RANGE_MESSAGE = `Round-up amount must be between £${MIN_ROUND_UP_AMOUNT.toFixed(2)} and £${MAX_ROUND_UP_AMOUNT.toFixed(2)}.`

export function computeRoundUpSuggestion(value: number): number {
  return toPence(Math.ceil(value) - value) / 100
}

export function roundsUp(banks: readonly BankOption[], bankId: string): boolean {
  return banks.find((bank) => bank.id === bankId)?.roundUpEnabled === true
}

export function suggestRoundUpAmount(banks: readonly BankOption[], bankId: string, value: string): string | null {
  if (!roundsUp(banks, bankId)) return null
  const parsedValue = Number(value)
  if (!value.trim() || !isFinite(parsedValue) || parsedValue <= 0) return null
  return computeRoundUpSuggestion(parsedValue).toFixed(2)
}

export function parseRoundUpAmount(value: string): number | null {
  return parseValidatedNumber(value, { min: MIN_ROUND_UP_AMOUNT, max: MAX_ROUND_UP_AMOUNT })
}
