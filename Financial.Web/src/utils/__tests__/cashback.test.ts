import { describe, expect, it } from 'vitest'
import type { BankOption } from '../../api/types'
import { cashbackRateOf, parseCashbackAmount, suggestCashbackAmount } from '../cashback'

const BANKS: BankOption[] = [
  { id: 'chase', name: 'Chase', roundUpEnabled: false, isActive: true, cashbackEnabled: true, cashbackRatePercent: 2 },
  { id: 't212', name: 'Trading 212', roundUpEnabled: false, isActive: true, cashbackEnabled: true, cashbackRatePercent: 1.5 },
  { id: 'full', name: 'Full', roundUpEnabled: false, isActive: true, cashbackEnabled: true, cashbackRatePercent: 100 },
  { id: 'barclays', name: 'Barclays', roundUpEnabled: false, isActive: true, cashbackEnabled: false, cashbackRatePercent: null },
]

describe('cashback', () => {
  it.each([
    ['chase', '12.50', '0.25'],
    ['chase', '12.99', '0.25'],
    ['t212', '9.99', '0.14'],
    ['full', '4.35', '4.35'],
  ])('[AC P65-F03-cashback-web-02] suggests %s cashback on %s as %s, rounded down to the penny', (bankId, value, expected) => {
    expect(suggestCashbackAmount(BANKS, bankId, value)).toBe(expected)
  })

  it.each([
    ['barclays', '12.50'],
    ['unknown', '12.50'],
    ['chase', ''],
    ['chase', '-5'],
    ['chase', 'abc'],
  ])('suggests nothing for bank %s and value "%s"', (bankId, value) => {
    expect(suggestCashbackAmount(BANKS, bankId, value)).toBeNull()
  })

  it('reads the rate only from a bank with cashback on', () => {
    expect(cashbackRateOf(BANKS, 'chase')).toBe(2)
    expect(cashbackRateOf(BANKS, 'barclays')).toBeNull()
  })

  it.each([
    ['0', 12.5, 0],
    ['12.50', 12.5, 12.5],
    ['0.250', 12.5, 0.25],
    ['12.51', 12.5, null],
    ['-0.01', 12.5, null],
    ['0.255', 12.5, null],
  ])('parses the cashback "%s" on a %s expense as %s', (cashback, value, expected) => {
    expect(parseCashbackAmount(cashback, value)).toBe(expected)
  })
})
