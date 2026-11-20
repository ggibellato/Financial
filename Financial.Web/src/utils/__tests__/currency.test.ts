import { describe, expect, it } from 'vitest'
import { isCurrency } from '../currency'

describe('currency', () => {
  it.each(['BRL', 'GBP', 'USD'])('recognises %s as a currency', (value) => {
    expect(isCurrency(value)).toBe(true)
  })

  it.each(['', 'EUR', 'gbp', 'ALL'])('rejects %j as a currency', (value) => {
    expect(isCurrency(value)).toBe(false)
  })
})
