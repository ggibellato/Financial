import { renderHook, waitFor } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { FinancialApiClient } from '../../api/financialApiClient'
import type { AccountStatementDto } from '../../api/types'
import { statementMonthOptions } from '../../utils/statementMonths'
import { useStatementMonthOptions } from '../useStatementMonthOptions'

const { getAccountStatementsMock } = vi.hoisted(() => ({
  getAccountStatementsMock: vi.fn<FinancialApiClient['getAccountStatements']>(),
}))

vi.mock('../../api/financialApiClient', () => ({
  apiClient: { getAccountStatements: getAccountStatementsMock } as Partial<FinancialApiClient>,
}))

const statement = (year: number, month: number, isPaid: boolean): AccountStatementDto => ({
  id: `s-${year}-${month}`,
  year,
  month,
  total: 10,
  creditIn: 0,
  outstandingTotal: isPaid ? 0 : 10,
  creditCarried: 0,
  status: isPaid ? 'Paid' : 'Unpaid',
  isPaid, settledOn: null,
  dueDate: null,
  paidOn: null,
  paidAmount: null,
  paidFromAccountName: null,
  potWithdrawals: [],
})

const values = (options: { value: string }[]) => options.map((option) => option.value)

describe('statementMonthOptions', () => {
  it('[AC P62-F03-withdraw-forms-on-pots-account-and-card-03] offers the expense month and the next three, leaving out paid ones', () => {
    const options = statementMonthOptions('2026-11-20', [statement(2026, 12, true), statement(2026, 11, false)], '')

    expect(options).toEqual([
      { value: '2026-11', label: 'November 2026' },
      { value: '2027-01', label: 'January 2027' },
      { value: '2027-02', label: 'February 2027' },
    ])
  })

  it('keeps the month an edited withdrawal is on, even outside the window', () => {
    expect(values(statementMonthOptions('2026-10-02', [], '2027-03'))).toEqual(['2026-10', '2026-11', '2026-12', '2027-01', '2027-03'])
  })

  it('offers nothing without a valid date', () => {
    expect(statementMonthOptions('', [], '')).toEqual([])
  })
})

describe('useStatementMonthOptions', () => {
  beforeEach(() => {
    getAccountStatementsMock.mockReset()
  })

  it("reads the card's statements and leaves out the paid months", async () => {
    getAccountStatementsMock.mockResolvedValue([statement(2026, 10, true), statement(2026, 11, false)])

    const { result } = renderHook(() => useStatementMonthOptions('amex', '2026-10-15', ''))

    await waitFor(() => expect(values(result.current.options)).toEqual(['2026-11', '2026-12', '2027-01']))
    expect(getAccountStatementsMock).toHaveBeenCalledWith('amex')
  })

  it('reads nothing and offers nothing without a card', () => {
    const { result } = renderHook(() => useStatementMonthOptions('', '2026-10-15', ''))

    expect(result.current.options).toEqual([])
    expect(getAccountStatementsMock).not.toHaveBeenCalled()
  })

  it('reports a failed read and reads again on retry', async () => {
    getAccountStatementsMock.mockRejectedValueOnce(new Error('down')).mockResolvedValue([])
    const { result } = renderHook(() => useStatementMonthOptions('amex', '2026-10-15', ''))
    await waitFor(() => expect(result.current.error).not.toBeNull())

    result.current.retry()

    await waitFor(() => expect(values(result.current.options)).toEqual(['2026-10', '2026-11', '2026-12', '2027-01']))
  })
})
