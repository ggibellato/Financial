import { formatMonthInputValue, todayIsoDate } from './formatters'
import { readStorage, writeStorage } from './safeStorage'

const STORAGE_PREFIX = 'financial.createFormDefault.'

export interface ViewedMonth {
  year: number
  month: number
}

export function getStoredDefault(key: string): string | null {
  return readStorage('session', STORAGE_PREFIX + key)
}

export function setStoredDefault(key: string, value: string): void {
  writeStorage('session', STORAGE_PREFIX + key, value)
}

function lastDayOfMonth({ year, month }: ViewedMonth): string {
  const lastDay = new Date(year, month, 0).getDate()
  return `${formatMonthInputValue(year, month)}-${String(lastDay).padStart(2, '0')}`
}

export function getCreateFormDate(dateKey: string, viewedMonth?: ViewedMonth): string {
  const lastUsed = getStoredDefault(dateKey)
  const today = todayIsoDate()
  if (!viewedMonth) return lastUsed ?? today

  const viewedMonthValue = formatMonthInputValue(viewedMonth.year, viewedMonth.month)
  if (lastUsed?.startsWith(`${viewedMonthValue}-`)) return lastUsed
  if (viewedMonthValue >= today.slice(0, 7)) return today
  return lastDayOfMonth(viewedMonth)
}
