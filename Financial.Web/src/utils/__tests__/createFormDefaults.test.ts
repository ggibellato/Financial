import { afterEach, describe, expect, it } from 'vitest'
import { getStoredDefault, setStoredDefault } from '../createFormDefaults'

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
