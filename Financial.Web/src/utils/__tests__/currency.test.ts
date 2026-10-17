import { describe, expect, it } from 'vitest'
import { CURRENCIES, REPORTING_CURRENCIES, isCurrency } from '../currency'

describe('currency', () => {
  it('lists every currency in alphabetical order', () => {
    expect(CURRENCIES).toEqual(['BRL', 'GBP', 'USD'])
  })

  it('lists every currency with GBP first for reporting', () => {
    expect(REPORTING_CURRENCIES).toEqual(['GBP', 'BRL', 'USD'])
  })

  it.each(['BRL', 'GBP', 'USD'])('recognises %s as a currency', (value) => {
    expect(isCurrency(value)).toBe(true)
  })

  it.each(['', 'EUR', 'gbp', 'ALL'])('rejects %j as a currency', (value) => {
    expect(isCurrency(value)).toBe(false)
  })
})
