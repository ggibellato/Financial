import { describe, expect, it } from 'vitest'
import { shownBalance } from '../owedBalance'

describe('shownBalance', () => {
  it('negates an owed balance', () => {
    expect(shownBalance(-812.33, true)).toBe(812.33)
  })

  it('shows nothing owed as 0, not -0', () => {
    expect(Object.is(shownBalance(0, true), 0)).toBe(true)
  })

  it('keeps a balance that is not owed as it is', () => {
    expect(shownBalance(-50, false)).toBe(-50)
  })
})
