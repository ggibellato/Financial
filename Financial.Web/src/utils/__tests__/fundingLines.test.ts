import { describe, expect, it } from 'vitest'
import { fundingLinesMismatch, remainingAmount, type FundingLineValues } from '../fundingLines'

const line = (amount: string): FundingLineValues => ({ key: amount, bankId: 'b1', amount })

describe('fundingLinesMismatch', () => {
  it('[AC P60-F10-linked-deposit-withdrawal-and-pots-screen-02] says how far the lines are from the deposit', () => {
    expect(fundingLinesMismatch([line('85'), line('3')], 90, 'GBP')).toBe('Lines add up to £88.00; the deposit is £90.00.')
  })

  it('[AC P60-F10-linked-deposit-withdrawal-and-pots-screen-02] says nothing when the lines add up to the penny', () => {
    expect(fundingLinesMismatch([line('85.10'), line('4.90')], 90, 'GBP')).toBeNull()
    expect(fundingLinesMismatch([line('0.10'), line('0.20')], 0.3, 'GBP')).toBeNull()
  })

  it('says nothing until the deposit has an amount', () => {
    expect(fundingLinesMismatch([line('85')], null, 'GBP')).toBeNull()
  })
})

describe('remainingAmount', () => {
  it('offers what the lines have not covered', () => {
    expect(remainingAmount([line('85')], 90)).toBe('5.00')
  })

  it('offers nothing when the lines already cover the deposit or it has no amount', () => {
    expect(remainingAmount([line('90')], 90)).toBe('')
    expect(remainingAmount([line('95')], 90)).toBe('')
    expect(remainingAmount([line('5')], null)).toBe('')
  })
})
