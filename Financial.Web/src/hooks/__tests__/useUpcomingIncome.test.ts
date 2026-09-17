import { renderHook, waitFor } from '@testing-library/react'
import { act } from 'react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { FinancialApiClient } from '../../api/financialApiClient'
import type { UpcomingIncomeDto } from '../../api/types'
import { useUpcomingIncome } from '../useUpcomingIncome'

const { getUpcomingIncomeMock } = vi.hoisted(() => ({
  getUpcomingIncomeMock: vi.fn<FinancialApiClient['getUpcomingIncome']>(),
}))

vi.mock('../../api/financialApiClient', () => ({
  apiClient: {
    getUpcomingIncome: getUpcomingIncomeMock,
  } as Partial<FinancialApiClient>,
}))

const ENTRIES: UpcomingIncomeDto[] = [
  {
    assetName: 'VUSA',
    brokerName: 'Trading212',
    lastCreditDate: '2026-08-15T00:00:00',
    projectedAmount: 42.5,
    projectedNextDate: '2026-09-15T00:00:00',
  },
]

describe('useUpcomingIncome', () => {
  beforeEach(() => {
    getUpcomingIncomeMock.mockReset()
  })

  it('fetches_the_projections_once_on_mount', async () => {
    getUpcomingIncomeMock.mockResolvedValue(ENTRIES)

    const { result } = renderHook(() => useUpcomingIncome())

    await waitFor(() => expect(result.current.entries).toEqual(ENTRIES))
    expect(getUpcomingIncomeMock).toHaveBeenCalledTimes(1)
    expect(result.current.isLoading).toBe(false)
    expect(result.current.error).toBeNull()
  })

  it('exposes_the_failure_message_and_no_entries_when_the_request_rejects', async () => {
    getUpcomingIncomeMock.mockRejectedValue(new Error('Service unavailable'))

    const { result } = renderHook(() => useUpcomingIncome())

    await waitFor(() => expect(result.current.error).toBe('Service unavailable'))
    expect(result.current.entries).toBeNull()
    expect(result.current.isLoading).toBe(false)
  })

  it('falls_back_to_an_upcoming_income_specific_message_for_a_non_error_rejection', async () => {
    getUpcomingIncomeMock.mockRejectedValue('boom')

    const { result } = renderHook(() => useUpcomingIncome())

    await waitFor(() => expect(result.current.error).toBe('Unable to load upcoming income'))
  })

  it('retry_re_issues_the_request', async () => {
    getUpcomingIncomeMock.mockRejectedValueOnce(new Error('Service unavailable')).mockResolvedValue(ENTRIES)

    const { result } = renderHook(() => useUpcomingIncome())
    await waitFor(() => expect(result.current.error).toBe('Service unavailable'))

    act(() => result.current.retry())

    await waitFor(() => expect(result.current.entries).toEqual(ENTRIES))
    expect(getUpcomingIncomeMock).toHaveBeenCalledTimes(2)
    expect(result.current.error).toBeNull()
  })
})
