import { afterEach, describe, expect, it } from 'vitest'
import { readStorage, writeStorage } from '../safeStorage'
import type { StorageArea } from '../safeStorage'

const STORAGE_PROPERTY: Record<StorageArea, 'localStorage' | 'sessionStorage'> = {
  local: 'localStorage',
  session: 'sessionStorage',
}

function withUnavailableStorage(area: StorageArea, run: () => void) {
  const property = STORAGE_PROPERTY[area]
  const original = window[property]
  Object.defineProperty(window, property, {
    configurable: true,
    get() {
      throw new Error(`${property} disabled`)
    },
  })
  try {
    run()
  } finally {
    Object.defineProperty(window, property, { configurable: true, value: original })
  }
}

describe('safeStorage', () => {
  afterEach(() => {
    localStorage.clear()
    sessionStorage.clear()
  })

  it('returns null for a key that was never written', () => {
    expect(readStorage('local', 'financial.test')).toBeNull()
  })

  it('writes to localStorage for the local area', () => {
    expect(writeStorage('local', 'financial.test', 'value')).toBe(true)

    expect(localStorage.getItem('financial.test')).toBe('value')
    expect(sessionStorage.getItem('financial.test')).toBeNull()
    expect(readStorage('local', 'financial.test')).toBe('value')
  })

  it('writes to sessionStorage for the session area', () => {
    expect(writeStorage('session', 'financial.test', 'value')).toBe(true)

    expect(sessionStorage.getItem('financial.test')).toBe('value')
    expect(localStorage.getItem('financial.test')).toBeNull()
    expect(readStorage('session', 'financial.test')).toBe('value')
  })

  it.each(['local', 'session'] as const)('swallows %s storage access errors', (area) => {
    withUnavailableStorage(area, () => {
      expect(readStorage(area, 'financial.test')).toBeNull()
      expect(writeStorage(area, 'financial.test', 'value')).toBe(false)
    })
  })
})
