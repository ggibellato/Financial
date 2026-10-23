import { act, renderHook, waitFor } from '@testing-library/react'
import type { ReactNode } from 'react'
import { MemoryRouter, Route, Routes, useLocation } from 'react-router-dom'
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { ApiError } from '../../api/apiError'
import type { FinancialApiClient } from '../../api/financialApiClient'
import type { AccountDetailDto, AccountDto, AccountTransactionDto } from '../../api/types'
import { pinDate } from '../../test-utils/pinDate'
import { useAccountScreen } from '../useAccountScreen'
import { accountTransactionRow } from '../../test/accountTransactionRow'

const { getAccountsMock, getAccountMock, getAccountTransactionsMock } = vi.hoisted(() => ({
  getAccountsMock: vi.fn<FinancialApiClient['getAccounts']>(),
  getAccountMock: vi.fn<FinancialApiClient['getAccount']>(),
  getAccountTransactionsMock: vi.fn<FinancialApiClient['getAccountTransactions']>(),
}))

vi.mock('../../api/financialApiClient', () => ({
  apiClient: {
    getAccounts: getAccountsMock,
    getAccount: getAccountMock,
    getAccountTransactions: getAccountTransactionsMock,
  } as Partial<FinancialApiClient>,
}))

const BARCLAYS_ID = '8f3b1c1a-2e3a-4b1a-9a7f-100000000001'

const ACCOUNTS: AccountDto[] = [{ id: BARCLAYS_ID, name: 'Barclays', templateName: 'Bank', currency: 'GBP', isActive: true }]

const DETAIL: AccountDetailDto = {
  id: BARCLAYS_ID,
  name: 'Barclays',
  templateName: 'Bank',
  currency: 'GBP',
  isActive: true,
  year: 2026,
  month: 9,
  balance: 1234.56,
  allowedActions: [],
}

const TRANSACTIONS: AccountTransactionDto[] = [
  accountTransactionRow({ id: 't1', kind: 'Income', date: '2026-09-01', amount: 100, canEdit: true }),
]

function renderAccountScreen(entry: string) {
  const wrapper = ({ children }: { children: ReactNode }) => (
    <MemoryRouter initialEntries={[entry]}>
      <Routes>
        <Route path="/cashflow/accounts/:accountId" element={children} />
      </Routes>
    </MemoryRouter>
  )
  return renderHook(() => ({ screen: useAccountScreen(), location: useLocation() }), { wrapper })
}

describe('useAccountScreen', () => {
  beforeEach(() => {
    pinDate('2026-10-15T12:00:00+01:00')
    getAccountsMock.mockReset().mockResolvedValue(ACCOUNTS)
    getAccountMock.mockReset().mockResolvedValue(DETAIL)
    getAccountTransactionsMock.mockReset().mockResolvedValue(TRANSACTIONS)
  })

  afterEach(() => {
    vi.useRealTimers()
  })

  it('reads the month from the URL and loads the list, detail and transactions together', async () => {
    const { result } = renderAccountScreen(`/cashflow/accounts/${BARCLAYS_ID}?month=2026-09`)

    expect(result.current.screen.isLoading).toBe(true)
    await waitFor(() => expect(result.current.screen.isLoading).toBe(false))

    expect(result.current.screen.monthInputValue).toBe('2026-09')
    expect(getAccountsMock).toHaveBeenCalledTimes(1)
    expect(getAccountMock).toHaveBeenCalledWith(BARCLAYS_ID, 2026, 9)
    expect(getAccountTransactionsMock).toHaveBeenCalledWith(BARCLAYS_ID, 2026, 9)
    expect(result.current.screen.accounts).toEqual(ACCOUNTS)
    expect(result.current.screen.account).toEqual(DETAIL)
    expect(result.current.screen.transactions).toEqual(TRANSACTIONS)
  })

  it('replaces a missing month with the current month in the URL', async () => {
    const { result } = renderAccountScreen(`/cashflow/accounts/${BARCLAYS_ID}`)

    await waitFor(() => expect(result.current.location.search).toBe('?month=2026-10'))
    await waitFor(() => expect(result.current.screen.isLoading).toBe(false))
    expect(getAccountMock).toHaveBeenCalledWith(BARCLAYS_ID, 2026, 10)
  })

  it('replaces an invalid month with the current month in the URL', async () => {
    const { result } = renderAccountScreen(`/cashflow/accounts/${BARCLAYS_ID}?month=2026-13`)

    await waitFor(() => expect(result.current.location.search).toBe('?month=2026-10'))
    expect(getAccountMock).not.toHaveBeenCalledWith(BARCLAYS_ID, 2026, 13)
  })

  it('reports not found when the account read returns 404', async () => {
    getAccountMock.mockRejectedValue(new ApiError('Account not found', 404))

    const { result } = renderAccountScreen(`/cashflow/accounts/${BARCLAYS_ID}?month=2026-10`)

    await waitFor(() => expect(result.current.screen.isNotFound).toBe(true))
    expect(result.current.screen.error).toBeNull()
  })

  it('reports not found for a malformed id without calling the API', () => {
    const { result } = renderAccountScreen('/cashflow/accounts/not-a-guid?month=2026-10')

    expect(result.current.screen.isNotFound).toBe(true)
    expect(result.current.screen.isLoading).toBe(false)
    expect(getAccountsMock).not.toHaveBeenCalled()
    expect(getAccountMock).not.toHaveBeenCalled()
  })

  it('reports an error on a failed load and reloads on retry', async () => {
    getAccountTransactionsMock.mockRejectedValueOnce(new ApiError('Server error', 500))

    const { result } = renderAccountScreen(`/cashflow/accounts/${BARCLAYS_ID}?month=2026-10`)

    await waitFor(() => expect(result.current.screen.error).not.toBeNull())
    expect(result.current.screen.isNotFound).toBe(false)

    act(() => result.current.screen.retry())

    await waitFor(() => expect(result.current.screen.transactions).toEqual(TRANSACTIONS))
    expect(result.current.screen.error).toBeNull()
  })

  it('refreshes silently without entering the loading state', async () => {
    const { result } = renderAccountScreen(`/cashflow/accounts/${BARCLAYS_ID}?month=2026-10`)
    await waitFor(() => expect(result.current.screen.isLoading).toBe(false))
    const updated = { ...DETAIL, balance: 999 }
    getAccountMock.mockResolvedValue(updated)

    let refresh: Promise<void> = Promise.resolve()
    act(() => {
      refresh = result.current.screen.refreshSilently()
    })

    expect(result.current.screen.isLoading).toBe(false)
    await act(() => refresh)
    expect(result.current.screen.account).toEqual(updated)
  })

  it('changes the month in the URL and reloads', async () => {
    const { result } = renderAccountScreen(`/cashflow/accounts/${BARCLAYS_ID}?month=2026-10`)
    await waitFor(() => expect(result.current.screen.isLoading).toBe(false))

    act(() => result.current.screen.setMonthInputValue('2026-08'))

    await waitFor(() => expect(getAccountMock).toHaveBeenCalledWith(BARCLAYS_ID, 2026, 8))
    expect(result.current.location.search).toBe('?month=2026-08')
  })

  it('ignores an empty month value', async () => {
    const { result } = renderAccountScreen(`/cashflow/accounts/${BARCLAYS_ID}?month=2026-10`)
    await waitFor(() => expect(result.current.screen.isLoading).toBe(false))

    act(() => result.current.screen.setMonthInputValue(''))

    expect(result.current.location.search).toBe('?month=2026-10')
  })

  it('navigates to another account keeping the month', async () => {
    const otherId = '8f3b1c1a-2e3a-4b1a-9a7f-100000000002'
    const { result } = renderAccountScreen(`/cashflow/accounts/${BARCLAYS_ID}?month=2026-09`)
    await waitFor(() => expect(result.current.screen.isLoading).toBe(false))

    act(() => result.current.screen.selectAccount(otherId))

    await waitFor(() => expect(result.current.location.pathname).toBe(`/cashflow/accounts/${otherId}`))
    expect(result.current.location.search).toBe('?month=2026-09')
  })
})
