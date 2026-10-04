import { describe, expect, it } from 'vitest'

describe('test environment', () => {
  it('runs in the Europe/London timezone', () => {
    expect(Intl.DateTimeFormat().resolvedOptions().timeZone).toBe('Europe/London')
  })

  it('formats dates and numbers as en-GB', () => {
    expect(new Date(2026, 6, 1).toLocaleDateString()).toBe('01/07/2026')
    expect((1234.5).toLocaleString()).toBe('1,234.5')
  })
})
