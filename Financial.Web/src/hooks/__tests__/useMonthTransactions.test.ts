import { act, renderHook, waitFor } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { FinancialApiClient } from '../../api/financialApiClient'
import type { MonthTransactionDto } from '../../api/types'
import { MONTH_TRANSACTIONS_LOAD_ERROR, useMonthTransactions } from '../useMonthTransactions'

const { getMonthTransactionsMock } = vi.hoisted(() => ({
  getMonthTransactionsMock: vi.fn<FinancialApiClient['getMonthTransactions']>(),
}))

vi.mock('../../api/financialApiClient', () => ({
  apiClient: { getMonthTransactions: getMonthTransactionsMock } as Partial<FinancialApiClient>,
}))

const ROWS = [{ accountId: 'a1', accountName: 'Barclays', accountCurrency: 'GBP', accountIsActive: true }] as MonthTransactionDto[]

describe('useMonthTransactions', () => {
  beforeEach(() => {
    getMonthTransactionsMock.mockReset().mockResolvedValue(ROWS)
  })

  it('starts loading and returns the month from one request', async () => {
    const { result } = renderHook(() => useMonthTransactions(2026, 10))

    expect(result.current.isLoading).toBe(true)
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    expect(getMonthTransactionsMock).toHaveBeenCalledTimes(1)
    expect(getMonthTransactionsMock).toHaveBeenCalledWith(2026, 10)
    expect(result.current.data).toEqual(ROWS)
  })

  it('reloads when the month changes', async () => {
    const { result, rerender } = renderHook(({ month }) => useMonthTransactions(2026, month), { initialProps: { month: 10 } })
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    rerender({ month: 11 })

    await waitFor(() => expect(getMonthTransactionsMock).toHaveBeenLastCalledWith(2026, 11))
    expect(getMonthTransactionsMock).toHaveBeenCalledTimes(2)
  })

  it('reports the load error and retries', async () => {
    getMonthTransactionsMock.mockRejectedValueOnce({ status: 503 })
    const { result } = renderHook(() => useMonthTransactions(2026, 10))

    await waitFor(() => expect(result.current.error).toBe(MONTH_TRANSACTIONS_LOAD_ERROR))
    expect(result.current.isLoading).toBe(false)

    act(() => result.current.retry())

    await waitFor(() => expect(result.current.data).toEqual(ROWS))
    expect(result.current.error).toBeNull()
  })
})
