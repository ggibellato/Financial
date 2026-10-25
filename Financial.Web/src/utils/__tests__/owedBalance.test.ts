import { describe, expect, it } from 'vitest'
import { balanceLabel, formatLabelledBalance, formatShownBalance } from '../owedBalance'

describe('owedBalance', () => {
  it('labels an owed balance "Owed" and any other "Balance"', () => {
    expect(balanceLabel(true)).toBe('Owed')
    expect(balanceLabel(false)).toBe('Balance')
  })

  it('shows an owed balance negated in the usual number format', () => {
    expect(formatShownBalance(-1812.3, true)).toBe('1,812.30')
    expect(formatLabelledBalance(-812.33, true)).toBe('Owed 812.33')
  })

  it('shows nothing owed as 0.00, not -0.00', () => {
    expect(formatShownBalance(0, true)).toBe('0.00')
    expect(formatLabelledBalance(0, true)).toBe('Owed 0.00')
  })

  it('keeps a balance that is not owed as it is, with no label', () => {
    expect(formatShownBalance(-50, false)).toBe('-50.00')
    expect(formatLabelledBalance(2800.25, false)).toBe('2,800.25')
  })
})
