import { renderHook, waitFor } from '@testing-library/react'
import { act } from 'react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { FinancialApiClient } from '../../api/financialApiClient'
import type { AllocationBreakdownDto } from '../../api/types'
import { useAllocationBreakdown } from '../useAllocationBreakdown'

const { getAllocationBreakdownMock } = vi.hoisted(() => ({
  getAllocationBreakdownMock: vi.fn<FinancialApiClient['getAllocationBreakdown']>(),
}))

vi.mock('../../api/financialApiClient', () => ({
  apiClient: {
    getAllocationBreakdown: getAllocationBreakdownMock,
  } as Partial<FinancialApiClient>,
}))

const BREAKDOWN: AllocationBreakdownDto = {
  byClass: [{ class: 'Equity', marketValue: 18000, percentage: 100 }],
  byCurrency: [{ currency: 'GBP', marketValue: 18000, percentage: 100 }],
  byCountry: [{ country: 'UK', marketValue: 18000, percentage: 100 }],
  byBroker: [{ brokerName: 'Trading212', marketValue: 18000, percentage: 100 }],
}

describe('useAllocationBreakdown', () => {
  beforeEach(() => {
    getAllocationBreakdownMock.mockReset()
  })

  it('fetches_the_allocation_breakdown_once_on_mount', async () => {
    getAllocationBreakdownMock.mockResolvedValue(BREAKDOWN)

    const { result } = renderHook(() => useAllocationBreakdown())

    await waitFor(() => expect(result.current.breakdown).toEqual(BREAKDOWN))
    expect(getAllocationBreakdownMock).toHaveBeenCalledTimes(1)
    expect(result.current.isLoading).toBe(false)
    expect(result.current.error).toBeNull()
  })

  it('reports_loading_until_the_request_resolves', async () => {
    getAllocationBreakdownMock.mockResolvedValue(BREAKDOWN)

    const { result } = renderHook(() => useAllocationBreakdown())

    await waitFor(() => expect(result.current.isLoading).toBe(true))
    await waitFor(() => expect(result.current.isLoading).toBe(false))
  })

  it('exposes_the_failure_message_and_no_breakdown_when_the_request_rejects', async () => {
    getAllocationBreakdownMock.mockRejectedValue(new Error('Service unavailable'))

    const { result } = renderHook(() => useAllocationBreakdown())

    await waitFor(() => expect(result.current.error).toBe('Service unavailable'))
    expect(result.current.breakdown).toBeNull()
    expect(result.current.isLoading).toBe(false)
  })

  it('falls_back_to_an_allocation_specific_message_for_a_non_error_rejection', async () => {
    getAllocationBreakdownMock.mockRejectedValue('boom')

    const { result } = renderHook(() => useAllocationBreakdown())

    await waitFor(() => expect(result.current.error).toBe('Unable to load allocation breakdown'))
  })

  it('retry_re_issues_the_request', async () => {
    getAllocationBreakdownMock.mockRejectedValueOnce(new Error('Service unavailable')).mockResolvedValue(BREAKDOWN)

    const { result } = renderHook(() => useAllocationBreakdown())
    await waitFor(() => expect(result.current.error).toBe('Service unavailable'))

    act(() => result.current.retry())

    await waitFor(() => expect(result.current.breakdown).toEqual(BREAKDOWN))
    expect(getAllocationBreakdownMock).toHaveBeenCalledTimes(2)
    expect(result.current.error).toBeNull()
  })
})
