import { afterEach, describe, expect, it } from 'vitest'
import { getStoredSidebarCollapsed, setStoredSidebarCollapsed } from '../sidebarStorage'

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
})
