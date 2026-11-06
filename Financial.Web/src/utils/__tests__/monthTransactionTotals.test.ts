import { describe, expect, it } from 'vitest'
import type { MonthTransactionDto } from '../../api/types'
import { monthTransactionTotals } from '../monthTransactionTotals'

function row(currency: string, amount: number): MonthTransactionDto {
  return {
    accountId: `account-${currency}`,
    accountName: currency,
    accountCurrency: currency,
    accountIsActive: true,
    transaction: { amount } as MonthTransactionDto['transaction'],
  }
}

describe('monthTransactionTotals', () => {
  it('[AC P60-F16-month-transactions-view-02] sums each currency into a Total and a Spend, currencies in code order', () => {
    const totals = monthTransactionTotals([row('GBP', -45.2), row('BRL', -100), row('GBP', -10.05), row('GBP', 5)])

    expect(totals).toEqual([
      { currency: 'BRL', total: -100, spend: 100 },
      { currency: 'GBP', total: -50.25, spend: 50.25 },
    ])
  })

  it('sums in pence so float error never reaches the figure', () => {
    const totals = monthTransactionTotals([row('GBP', -0.1), row('GBP', -0.2), row('GBP', -0.7)])

    expect(totals).toEqual([{ currency: 'GBP', total: -1, spend: 1 }])
  })

  it('reports a net refund as a negative Spend', () => {
    expect(monthTransactionTotals([row('GBP', 20), row('GBP', -5)])).toEqual([{ currency: 'GBP', total: 15, spend: -15 }])
  })

  it('reports a zero sum as zero, never as negative zero', () => {
    const [total] = monthTransactionTotals([row('GBP', -5), row('GBP', 5)])

    expect(Object.is(total.total, 0)).toBe(true)
    expect(Object.is(total.spend, 0)).toBe(true)
  })

  it('returns no totals for no rows', () => {
    expect(monthTransactionTotals([])).toEqual([])
  })
})
