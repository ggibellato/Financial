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

interface Props {
  month: number
  isOpen: boolean
}

function renderMonthTransactions(initialProps: Props) {
  return renderHook(({ month, isOpen }: Props) => useMonthTransactions(2026, month, isOpen), { initialProps })
}

describe('useMonthTransactions', () => {
  beforeEach(() => {
    getMonthTransactionsMock.mockReset().mockResolvedValue(ROWS)
  })

  it('requests nothing while the tab is closed', () => {
    const { result } = renderMonthTransactions({ month: 10, isOpen: false })

    expect(getMonthTransactionsMock).not.toHaveBeenCalled()
    expect(result.current.isLoading).toBe(false)
    expect(result.current.rows).toEqual([])
  })

  it('loads the month when the tab opens', async () => {
    const { result, rerender } = renderMonthTransactions({ month: 10, isOpen: false })

    rerender({ month: 10, isOpen: true })

    expect(result.current.isLoading).toBe(true)
    await waitFor(() => expect(result.current.isLoading).toBe(false))
    expect(getMonthTransactionsMock).toHaveBeenCalledWith(2026, 10)
    expect(result.current.rows).toEqual(ROWS)
  })

  it('reloads when the month changes while open', async () => {
    const { result, rerender } = renderMonthTransactions({ month: 10, isOpen: true })
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    rerender({ month: 11, isOpen: true })

    await waitFor(() => expect(getMonthTransactionsMock).toHaveBeenLastCalledWith(2026, 11))
    expect(getMonthTransactionsMock).toHaveBeenCalledTimes(2)
  })

  it('returns to idle when closed and loads fresh rows when reopened', async () => {
    const { result, rerender } = renderMonthTransactions({ month: 10, isOpen: true })
    await waitFor(() => expect(result.current.rows).toEqual(ROWS))

    rerender({ month: 10, isOpen: false })

    expect(result.current.rows).toEqual([])
    expect(result.current.isLoading).toBe(false)

    rerender({ month: 10, isOpen: true })

    await waitFor(() => expect(result.current.rows).toEqual(ROWS))
    expect(getMonthTransactionsMock).toHaveBeenCalledTimes(2)
  })

  it('reports the load error and retries', async () => {
    getMonthTransactionsMock.mockRejectedValueOnce({ status: 503 })
    const { result } = renderMonthTransactions({ month: 10, isOpen: true })

    await waitFor(() => expect(result.current.error).toBe(MONTH_TRANSACTIONS_LOAD_ERROR))
    expect(result.current.isLoading).toBe(false)

    act(() => result.current.retry())

    await waitFor(() => expect(result.current.rows).toEqual(ROWS))
    expect(result.current.error).toBeNull()
  })
})
