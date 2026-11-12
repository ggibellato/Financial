import { act, renderHook, waitFor } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { FinancialApiClient } from '../../api/financialApiClient'
import { ApiError } from '../../api/apiError'
import type { IncomeDto } from '../../api/types'
import { account, cardAccount } from '../../test/account'
import { useIncomeTab } from '../useIncomeTab'

const { getIncomesByMonthMock, getIncomeSourcesMock, getAccountsMock, deleteIncomeMock, showSuccessMock } = vi.hoisted(() => ({
  getIncomesByMonthMock: vi.fn<FinancialApiClient['getIncomesByMonth']>(),
  getIncomeSourcesMock: vi.fn<FinancialApiClient['getIncomeSources']>(),
  getAccountsMock: vi.fn<FinancialApiClient['getAccounts']>(),
  deleteIncomeMock: vi.fn<FinancialApiClient['deleteIncome']>(),
  showSuccessMock: vi.fn<(message: string) => void>(),
}))

vi.mock('../../api/financialApiClient', () => ({
  apiClient: {
    getIncomesByMonth: getIncomesByMonthMock,
    getIncomeSources: getIncomeSourcesMock,
    getAccounts: getAccountsMock,
    deleteIncome: deleteIncomeMock,
  } as Partial<FinancialApiClient>,
}))

vi.mock('../useSuccessToast', () => ({ useSuccessToast: () => ({ showSuccess: showSuccessMock }) }))

const INCOME: IncomeDto = {
  id: 'i1',
  date: '2026-10-15',
  incomeSourceId: 's1',
  incomeSourceName: 'DividendoJuros',
  grossValue: null,
  netValue: 85.2,
  bankId: null,
  bankName: null,
  description: null,
  potsAccountId: null,
  hasLinkedDeposit: false,
  fundingLines: [],
}

const ACCOUNTS = [
  account({ id: 'barclays', name: 'Barclays' }),
  account({ id: 'old', name: 'Old bank', isActive: false }),
  cardAccount({ id: 'amex', name: 'Amex' }),
]

async function renderLoaded(year = 2026, month = 10) {
  const rendered = renderHook(({ y, m }) => useIncomeTab(y, m), { initialProps: { y: year, m: month } })
  await waitFor(() => expect(rendered.result.current.isLoading).toBe(false))
  return rendered
}

describe('useIncomeTab', () => {
  beforeEach(() => {
    getIncomesByMonthMock.mockReset().mockResolvedValue([INCOME])
    getIncomeSourcesMock.mockReset().mockResolvedValue([])
    getAccountsMock.mockReset().mockResolvedValue(ACCOUNTS)
    deleteIncomeMock.mockReset()
    showSuccessMock.mockReset()
  })

  it('[AC P61-F02-income-tab-and-bankless-income-01] loads the month once and again only for a new month', async () => {
    const { result, rerender } = await renderLoaded()

    expect(result.current.incomes).toEqual([INCOME])
    expect(getIncomesByMonthMock).toHaveBeenCalledExactlyOnceWith(2026, 10)

    rerender({ y: 2026, m: 9 })
    await waitFor(() => expect(getIncomesByMonthMock).toHaveBeenLastCalledWith(2026, 9))
    expect(getIncomesByMonthMock).toHaveBeenCalledTimes(2)
  })

  it('[AC P61-F02-income-tab-and-bankless-income-03] offers the accounts that receive income, keeping inactive ones for the form to filter', async () => {
    const { result } = await renderLoaded()

    expect(result.current.banks.map((bank) => bank.id)).toEqual(['barclays', 'old'])
  })

  it('[AC P61-F02-income-tab-and-bankless-income-04] refreshes the list and toasts after a delete', async () => {
    deleteIncomeMock.mockResolvedValue()
    const { result } = await renderLoaded()

    act(() => result.current.incomeDelete.request(INCOME))
    act(() => result.current.incomeDelete.confirm())

    await waitFor(() => expect(showSuccessMock).toHaveBeenCalledWith('Income deleted.'))
    expect(getIncomesByMonthMock).toHaveBeenCalledTimes(2)
    expect(result.current.isGoneNoticeShown).toBe(false)
  })

  it('[AC P61-F02-income-tab-and-bankless-income-06] refreshes and shows the gone notice, without a toast, when the income no longer exists', async () => {
    deleteIncomeMock.mockRejectedValue(new ApiError('Not found.', 404))
    const { result } = await renderLoaded()

    act(() => result.current.incomeDelete.request(INCOME))
    act(() => result.current.incomeDelete.confirm())

    await waitFor(() => expect(result.current.isGoneNoticeShown).toBe(true))
    expect(result.current.incomeDelete.isOpen).toBe(false)
    expect(showSuccessMock).not.toHaveBeenCalled()
    expect(getIncomesByMonthMock).toHaveBeenCalledTimes(2)

    act(() => result.current.dismissGoneNotice())
    expect(result.current.isGoneNoticeShown).toBe(false)
  })

  it('[AC P61-F02-income-tab-and-bankless-income-06] keeps the dialog open with the message on any other delete failure', async () => {
    deleteIncomeMock.mockRejectedValue(new ApiError('Server unavailable.', 500))
    const { result } = await renderLoaded()

    act(() => result.current.incomeDelete.request(INCOME))
    act(() => result.current.incomeDelete.confirm())

    await waitFor(() => expect(result.current.incomeDelete.error).toBe('Server unavailable.'))
    expect(result.current.incomeDelete.isOpen).toBe(true)
    expect(result.current.isGoneNoticeShown).toBe(false)
    expect(getIncomesByMonthMock).toHaveBeenCalledOnce()
  })

  it('shows the load error with the fallback message when the request gives none', async () => {
    getIncomesByMonthMock.mockRejectedValue('offline')
    const { result } = await renderLoaded()

    expect(result.current.error).toBe('Unable to load the incomes.')
  })
})
