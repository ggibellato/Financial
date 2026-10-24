import { formatMonthInputValue, lastDayOfMonthIsoDate, todayIsoDate } from './formatters'
import { readStorage, writeStorage } from './safeStorage'

const STORAGE_PREFIX = 'financial.createFormDefault.'

export function getStoredDefault(key: string): string | null {
  return readStorage('session', STORAGE_PREFIX + key)
}

export function setStoredDefault(key: string, value: string): void {
  writeStorage('session', STORAGE_PREFIX + key, value)
}

export function getCreateFormDate(dateKey: string, viewedMonth?: { year: number; month: number }): string {
  const lastUsed = getStoredDefault(dateKey)
  const today = todayIsoDate()
  if (!viewedMonth) return lastUsed ?? today

  const viewedMonthValue = formatMonthInputValue(viewedMonth.year, viewedMonth.month)
  if (lastUsed?.startsWith(`${viewedMonthValue}-`)) return lastUsed
  if (viewedMonthValue >= today.slice(0, 7)) return today
  return lastDayOfMonthIsoDate(viewedMonth.year, viewedMonth.month)
}
