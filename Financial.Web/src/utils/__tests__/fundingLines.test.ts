import { describe, expect, it } from 'vitest'
import {
  MAX_FUNDING_LINES,
  fundingLineErrors,
  fundingLinesMismatch,
  incomeDepositAmount,
  newFundingLine,
  remainingAmount,
  withAddedLine,
  withChangedLine,
  withoutLine,
  type FundingLineValues,
} from '../fundingLines'

const line = (amount: string): FundingLineValues => ({ key: amount, bankId: 'b1', amount })

describe('incomeDepositAmount', () => {
  it.each([
    [100, 90],
    [123.45, 111.11],
    [0.05, 0.05],
    [1963, 1766.7],
    [100.005, 90],
    [0, 0],
  ])('takes 90%% of %d, rounding a half penny away from zero as the server does', (net, deposit) => {
    expect(incomeDepositAmount(net)).toBe(deposit)
  })
})

describe('fundingLineErrors', () => {
  it('names the bank and the amount of each incomplete line', () => {
    expect(fundingLineErrors([{ key: 'a', bankId: '', amount: '0' }, { key: 'b', bankId: 'b1', amount: '5' }])).toEqual({
      'line:a:bankId': 'Choose a bank.',
      'line:a:amount': 'Amount must be greater than zero.',
    })
  })
})

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

describe('newFundingLine', () => {
  it('starts empty, with a key of its own', () => {
    const first = newFundingLine()
    const second = newFundingLine()

    expect(first).toMatchObject({ bankId: '', amount: '' })
    expect(first.key).not.toBe(second.key)
  })
})

describe('withChangedLine', () => {
  it('changes only the line with the key', () => {
    const lines = [line('10'), line('20')]

    expect(withChangedLine(lines, '20', { bankId: 'b2' })).toEqual([line('10'), { key: '20', bankId: 'b2', amount: '20' }])
  })
})

describe('withAddedLine', () => {
  it('adds a line holding what the others have not covered', () => {
    const added = withAddedLine([line('30')], 100)

    expect(added).toHaveLength(2)
    expect(added[1]).toMatchObject({ bankId: '', amount: '70.00' })
  })

  it('adds no line past the limit', () => {
    const lines = Array.from({ length: MAX_FUNDING_LINES }, (_, i) => line(String(i + 1)))

    expect(withAddedLine(lines, 100)).toEqual(lines)
  })
})

describe('withoutLine', () => {
  it('removes only the line with the key', () => {
    expect(withoutLine([line('10'), line('20')], '10')).toEqual([line('20')])
  })
})
