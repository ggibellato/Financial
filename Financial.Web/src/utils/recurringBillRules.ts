import { parseValidatedNumber } from './formatters'

const MIN_DUE_DAY = 1
const MAX_DUE_DAY = 31

export const DUE_DAY_MESSAGE = `Due day must be between ${MIN_DUE_DAY} and ${MAX_DUE_DAY}.`

export function parseDueDay(value: string): number | null {
  const dueDay = parseValidatedNumber(value, { min: MIN_DUE_DAY, max: MAX_DUE_DAY })
  return dueDay !== null && Number.isInteger(dueDay) ? dueDay : null
}
