import { afterEach, describe, expect, it } from 'vitest'
import { getStoredDomain, setStoredDomain } from './domainStorage'

describe('domainStorage', () => {
  afterEach(() => {
    sessionStorage.clear()
  })

  it('returns null when nothing is stored', () => {
    expect(getStoredDomain()).toBeNull()
  })

  it('round-trips a stored domain', () => {
    setStoredDomain('cashflow')
    expect(getStoredDomain()).toBe('cashflow')

    setStoredDomain('investments')
    expect(getStoredDomain()).toBe('investments')
  })

  it('returns null for an unrecognized stored value', () => {
    sessionStorage.setItem('financial.selectedDomain', 'not-a-domain')
    expect(getStoredDomain()).toBeNull()
  })

  it('does not throw when sessionStorage access fails', () => {
    const original = window.sessionStorage
    Object.defineProperty(window, 'sessionStorage', {
      configurable: true,
      get() {
        throw new Error('sessionStorage disabled')
      },
    })

    expect(() => setStoredDomain('cashflow')).not.toThrow()
    expect(getStoredDomain()).toBeNull()

    Object.defineProperty(window, 'sessionStorage', {
      configurable: true,
      value: original,
    })
  })
})
