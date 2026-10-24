import { afterEach, describe, expect, it, vi } from 'vitest'
import { pinDate } from '../../test-utils/pinDate'
import { getCreateFormDate, getStoredDefault, setStoredDefault } from '../createFormDefaults'

describe('createFormDefaults', () => {
  afterEach(() => {
    sessionStorage.clear()
  })

  it('returns null when nothing is stored for a key', () => {
    expect(getStoredDefault('expense.date')).toBeNull()
  })

  it('round-trips a stored value', () => {
    setStoredDefault('expense.date', '2026-08-30')
    expect(getStoredDefault('expense.date')).toBe('2026-08-30')

    setStoredDefault('expense.date', '2026-09-01')
    expect(getStoredDefault('expense.date')).toBe('2026-09-01')
  })

  it('keeps different keys independent', () => {
    setStoredDefault('expense.date', '2026-08-30')
    setStoredDefault('income.date', '2026-08-01')

    expect(getStoredDefault('expense.date')).toBe('2026-08-30')
    expect(getStoredDefault('income.date')).toBe('2026-08-01')
  })

  it('does not throw when sessionStorage access fails', () => {
    const original = window.sessionStorage
    Object.defineProperty(window, 'sessionStorage', {
      configurable: true,
      get() {
        throw new Error('sessionStorage disabled')
      },
    })

    expect(() => setStoredDefault('expense.date', '2026-08-30')).not.toThrow()
    expect(getStoredDefault('expense.date')).toBeNull()

    Object.defineProperty(window, 'sessionStorage', {
      configurable: true,
      value: original,
    })
  })
})

describe('getCreateFormDate', () => {
  afterEach(() => {
    sessionStorage.clear()
    vi.useRealTimers()
  })

  it('keeps the last-used date, or today, when no month is being viewed', () => {
    pinDate('2026-10-15T12:00:00+01:00')
    expect(getCreateFormDate('expense.date')).toBe('2026-10-15')

    setStoredDefault('expense.date', '2026-08-30')
    expect(getCreateFormDate('expense.date')).toBe('2026-08-30')
  })

  it('keeps the last-used date when it falls in the viewed month', () => {
    pinDate('2026-10-15T12:00:00+01:00')
    setStoredDefault('expense.date', '2026-08-30')

    expect(getCreateFormDate('expense.date', { year: 2026, month: 8 })).toBe('2026-08-30')
  })

  it('uses today when viewing the current month and the last-used date is in another month', () => {
    pinDate('2026-10-15T12:00:00+01:00')
    setStoredDefault('expense.date', '2026-08-30')

    expect(getCreateFormDate('expense.date', { year: 2026, month: 10 })).toBe('2026-10-15')
  })

  it.each([
    [{ year: 2026, month: 9 }, '2026-09-30'],
    [{ year: 2026, month: 2 }, '2026-02-28'],
    [{ year: 2024, month: 2 }, '2024-02-29'],
    [{ year: 2025, month: 12 }, '2025-12-31'],
  ])('uses the last day of a past viewed month %o', (viewedMonth, expected) => {
    pinDate('2026-10-15T12:00:00+01:00')
    setStoredDefault('expense.date', '2026-10-01')

    expect(getCreateFormDate('expense.date', viewedMonth)).toBe(expected)
  })

  it('uses today when viewing a future month, since future dates are rejected', () => {
    pinDate('2026-10-15T12:00:00+01:00')

    expect(getCreateFormDate('expense.date', { year: 2027, month: 1 })).toBe('2026-10-15')
  })
})
