import { vi } from 'vitest'

export const PINNED_NOW = '2026-09-17T12:00:00+01:00'

export function inDays(days: number): string {
  const date = new Date(PINNED_NOW)
  date.setDate(date.getDate() + days)
  return date.toISOString()
}

export function pinDate(instant: string) {
  vi.useFakeTimers({ toFake: ['Date'] })
  vi.setSystemTime(new Date(instant))
}
