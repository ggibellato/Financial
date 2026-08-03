import { afterEach, describe, expect, it } from 'vitest'
import { getStoredSidebarCollapsed, setStoredSidebarCollapsed } from './sidebarStorage'

describe('sidebarStorage', () => {
  afterEach(() => {
    localStorage.clear()
  })

  it('returns false when nothing is stored', () => {
    expect(getStoredSidebarCollapsed()).toBe(false)
  })

  it('round-trips a stored value', () => {
    setStoredSidebarCollapsed(true)
    expect(getStoredSidebarCollapsed()).toBe(true)
    expect(localStorage.getItem('financial.sidebarCollapsed')).toBe('true')

    setStoredSidebarCollapsed(false)
    expect(getStoredSidebarCollapsed()).toBe(false)
  })

  it('returns false and does not throw when localStorage read fails', () => {
    const original = window.localStorage
    Object.defineProperty(window, 'localStorage', {
      configurable: true,
      get() {
        throw new Error('localStorage disabled')
      },
    })

    expect(() => getStoredSidebarCollapsed()).not.toThrow()
    expect(getStoredSidebarCollapsed()).toBe(false)

    Object.defineProperty(window, 'localStorage', {
      configurable: true,
      value: original,
    })
  })

  it('does not throw when localStorage write fails', () => {
    const original = window.localStorage
    Object.defineProperty(window, 'localStorage', {
      configurable: true,
      get() {
        throw new Error('localStorage disabled')
      },
    })

    expect(() => setStoredSidebarCollapsed(true)).not.toThrow()

    Object.defineProperty(window, 'localStorage', {
      configurable: true,
      value: original,
    })
  })
})
