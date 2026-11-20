import { act, renderHook, screen, waitFor } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { FinancialApiClient } from '../../api/financialApiClient'
import { ApiError } from '../../api/apiError'
import type { IncomeDto } from '../../api/types'
import { account, cardAccount } from '../../test/account'
import { setStoredDefault } from '../../utils/createFormDefaults'
import { WithAppToaster } from '../../test/withAppToaster'
import { useIncomeTab } from '../useIncomeTab'

const { getIncomesByMonthMock, getIncomeSourcesMock, getAccountsMock, deleteIncomeMock } = vi.hoisted(() => ({
  getIncomesByMonthMock: vi.fn<FinancialApiClient['getIncomesByMonth']>(),
  getIncomeSourcesMock: vi.fn<FinancialApiClient['getIncomeSources']>(),
  getAccountsMock: vi.fn<FinancialApiClient['getAccounts']>(),
  deleteIncomeMock: vi.fn<FinancialApiClient['deleteIncome']>(),
}))

vi.mock('../../api/financialApiClient', () => ({
  apiClient: {
    getIncomesByMonth: getIncomesByMonthMock,
    getIncomeSources: getIncomeSourcesMock,
    getAccounts: getAccountsMock,
    deleteIncome: deleteIncomeMock,
  } as Partial<FinancialApiClient>,
}))

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
  account({ id: 'old', name: 'Old bank', isActive: false }),
  account({ id: 'barclays', name: 'Barclays' }),
  cardAccount({ id: 'amex', name: 'Amex' }),
]

async function renderLoaded(year = 2026, month = 10) {
  const rendered = renderHook(({ y, m }) => useIncomeTab(y, m), { initialProps: { y: year, m: month }, wrapper: WithAppToaster })
  await waitFor(() => expect(rendered.result.current.isLoading).toBe(false))
  return rendered
}

describe('useIncomeTab', () => {
  beforeEach(() => {
    getIncomesByMonthMock.mockReset().mockResolvedValue([INCOME])
    getIncomeSourcesMock.mockReset().mockResolvedValue([])
    getAccountsMock.mockReset().mockResolvedValue(ACCOUNTS)
    deleteIncomeMock.mockReset()
    sessionStorage.clear()
  })

  it('[AC P61-F02-income-tab-and-bankless-income-01] loads the month once and again only for a new month', async () => {
    const { result, rerender } = await renderLoaded()

    expect(result.current.incomes).toEqual([INCOME])
    expect(getIncomesByMonthMock).toHaveBeenCalledExactlyOnceWith(2026, 10)

    rerender({ y: 2026, m: 9 })
    await waitFor(() => expect(getIncomesByMonthMock).toHaveBeenLastCalledWith(2026, 9))
    expect(getIncomesByMonthMock).toHaveBeenCalledTimes(2)
  })

  it('shows the loading state from the first render', () => {
    const seen: boolean[] = []

    renderHook(() => {
      seen.push(useIncomeTab(2026, 10).isLoading)
    })

    expect(seen[0]).toBe(true)
  })

  it('opens a new income with no bank, even with a bank remembered from an earlier save', async () => {
    setStoredDefault('income.bank', 'barclays')
    const { result } = await renderLoaded()

    act(() => result.current.openCreate())

    expect(result.current.incomeForm.incomeBank).toBe('')
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

    expect(await screen.findByText('Income deleted')).toBeInTheDocument()
    expect(getIncomesByMonthMock).toHaveBeenCalledTimes(2)
    expect(result.current.isGoneNoticeShown).toBe(false)
  })

  it.each([
    ['closes the form on the deleted income', INCOME, null],
    ['keeps the form on another income', { ...INCOME, id: 'i2' }, 'i2'],
  ])('%s after a delete', async (_, edited, editingId) => {
    deleteIncomeMock.mockResolvedValue()
    const { result } = await renderLoaded()
    act(() => result.current.openEdit(edited))

    act(() => result.current.incomeDelete.request(INCOME))
    act(() => result.current.incomeDelete.confirm())

    expect(await screen.findByText('Income deleted')).toBeInTheDocument()
    expect(result.current.incomeForm.editingIncomeId).toBe(editingId)
  })

  it('[AC P61-F02-income-tab-and-bankless-income-06] refreshes and shows the gone notice, without a toast, when the income no longer exists', async () => {
    deleteIncomeMock.mockRejectedValue(new ApiError('Not found.', 404))
    const { result } = await renderLoaded()

    act(() => result.current.incomeDelete.request(INCOME))
    act(() => result.current.incomeDelete.confirm())

    await waitFor(() => expect(result.current.isGoneNoticeShown).toBe(true))
    expect(result.current.incomeDelete.isOpen).toBe(false)
    expect(screen.queryByText('Income deleted')).not.toBeInTheDocument()
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
