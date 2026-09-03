import { afterEach, describe, expect, it } from 'vitest'
import { getStoredColourMode, setStoredColourMode } from '../colourModeStorage'

describe('colourModeStorage', () => {
  afterEach(() => {
    localStorage.clear()
  })

  it('returns_light_when_nothing_stored', () => {
    expect(getStoredColourMode()).toBe('light')
  })

  it('round_trips_a_stored_value', () => {
    setStoredColourMode('dark')
    expect(getStoredColourMode()).toBe('dark')
    expect(localStorage.getItem('financial.colourMode')).toBe('dark')

    setStoredColourMode('light')
    expect(getStoredColourMode()).toBe('light')
  })

  it('returns_light_and_does_not_throw_when_localStorage_read_fails', () => {
    const original = window.localStorage
    Object.defineProperty(window, 'localStorage', {
      configurable: true,
      get() {
        throw new Error('localStorage disabled')
      },
    })

    expect(() => getStoredColourMode()).not.toThrow()
    expect(getStoredColourMode()).toBe('light')

    Object.defineProperty(window, 'localStorage', {
      configurable: true,
      value: original,
    })
  })

  it('does_not_throw_when_localStorage_write_fails', () => {
    const original = window.localStorage
    Object.defineProperty(window, 'localStorage', {
      configurable: true,
      get() {
        throw new Error('localStorage disabled')
      },
    })

    expect(() => setStoredColourMode('dark')).not.toThrow()

    Object.defineProperty(window, 'localStorage', {
      configurable: true,
      value: original,
    })
  })
})
