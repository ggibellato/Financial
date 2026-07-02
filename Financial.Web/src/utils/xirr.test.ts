import { describe, expect, it } from 'vitest'
import { xirr } from './xirr'

describe('xirr', () => {
  it('xirr_returns_correct_annualised_rate_for_known_inputs', () => {
    // Buy 1000 on day 0, receive 1100 exactly one year later → XIRR = 10%
    const cashFlows = [
      { date: new Date('2021-01-01'), amount: -1000 },
      { date: new Date('2022-01-01'), amount: 1100 },
    ]
    const result = xirr(cashFlows)
    expect(result).not.toBeNull()
    expect(result!).toBeCloseTo(0.1, 4)
  })

  it('xirr_returns_null_when_fewer_than_two_cash_flows', () => {
    const cashFlows = [{ date: new Date('2021-01-01'), amount: -1000 }]
    expect(xirr(cashFlows)).toBeNull()
  })

  it('xirr_returns_null_when_series_is_empty', () => {
    expect(xirr([])).toBeNull()
  })

  it('xirr_returns_null_when_algorithm_does_not_converge', () => {
    // All cash flows on the same date → derivative is 0 → cannot converge
    const cashFlows = [
      { date: new Date('2021-01-01'), amount: -500 },
      { date: new Date('2021-01-01'), amount: -500 },
    ]
    expect(xirr(cashFlows)).toBeNull()
  })
})
