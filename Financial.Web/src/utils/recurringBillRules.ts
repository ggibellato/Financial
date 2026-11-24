import { parseIntegerInRange } from './formatters'

export const MIN_DUE_DAY = 1
export const MAX_DUE_DAY = 31

export const DUE_DAY_MESSAGE = `Due day must be between ${MIN_DUE_DAY} and ${MAX_DUE_DAY}.`

export function parseDueDay(value: string): number | null {
  return parseIntegerInRange(value, MIN_DUE_DAY, MAX_DUE_DAY)
}
