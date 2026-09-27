import { renderHook, waitFor } from '@testing-library/react'
import { act } from 'react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { FinancialApiClient } from '../../api/financialApiClient'
import type { AllocationBreakdownDto, BrokerCurrencyFilter, Currency } from '../../api/types'
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
  displayCurrency: 'GBP',
  isPartial: false,
  isUnavailable: false,
}

function renderBreakdownHook(displayCurrency: Currency | null, brokerCurrencyFilter: BrokerCurrencyFilter = 'ALL') {
  return renderHook(
    ({ currency, filter }: { currency: Currency | null; filter: BrokerCurrencyFilter }) =>
      useAllocationBreakdown(currency, filter),
    { initialProps: { currency: displayCurrency, filter: brokerCurrencyFilter } },
  )
}

describe('useAllocationBreakdown', () => {
  beforeEach(() => {
    getAllocationBreakdownMock.mockReset()
  })

  it('fetches_the_allocation_breakdown_once_on_mount', async () => {
    getAllocationBreakdownMock.mockResolvedValue(BREAKDOWN)

    const { result } = renderBreakdownHook('GBP')

    await waitFor(() => expect(result.current.breakdown).toEqual(BREAKDOWN))
    expect(getAllocationBreakdownMock).toHaveBeenCalledTimes(1)
    expect(result.current.isLoading).toBe(false)
    expect(result.current.error).toBeNull()
  })

  it('reports_loading_until_the_request_resolves', async () => {
    getAllocationBreakdownMock.mockResolvedValue(BREAKDOWN)

    const { result } = renderBreakdownHook('GBP')

    await waitFor(() => expect(result.current.isLoading).toBe(true))
    await waitFor(() => expect(result.current.isLoading).toBe(false))
  })

  it('exposes_the_failure_message_and_no_breakdown_when_the_request_rejects', async () => {
    getAllocationBreakdownMock.mockRejectedValue(new Error('Service unavailable'))

    const { result } = renderBreakdownHook('GBP')

    await waitFor(() => expect(result.current.error).toBe('Service unavailable'))
    expect(result.current.breakdown).toBeNull()
    expect(result.current.isLoading).toBe(false)
  })

  it('falls_back_to_an_allocation_specific_message_for_a_non_error_rejection', async () => {
    getAllocationBreakdownMock.mockRejectedValue('boom')

    const { result } = renderBreakdownHook('GBP')

    await waitFor(() => expect(result.current.error).toBe('Unable to load allocation breakdown'))
  })

  it('retry_re_issues_the_request', async () => {
    getAllocationBreakdownMock.mockRejectedValueOnce(new Error('Service unavailable')).mockResolvedValue(BREAKDOWN)

    const { result } = renderBreakdownHook('GBP')
    await waitFor(() => expect(result.current.error).toBe('Service unavailable'))

    act(() => result.current.retry())

    await waitFor(() => expect(result.current.breakdown).toEqual(BREAKDOWN))
    expect(getAllocationBreakdownMock).toHaveBeenCalledTimes(2)
    expect(result.current.error).toBeNull()
  })

  it('does_not_fetch_while_displayCurrency_is_null', () => {
    const { result } = renderBreakdownHook(null)

    expect(getAllocationBreakdownMock).not.toHaveBeenCalled()
    expect(result.current.isLoading).toBe(false)
    expect(result.current.breakdown).toBeNull()
  })

  it('fetches_once_displayCurrency_is_seeded', async () => {
    getAllocationBreakdownMock.mockResolvedValue(BREAKDOWN)

    const { result, rerender } = renderBreakdownHook(null)
    expect(getAllocationBreakdownMock).not.toHaveBeenCalled()

    rerender({ currency: 'GBP', filter: 'ALL' })

    await waitFor(() => expect(result.current.breakdown).toEqual(BREAKDOWN))
    expect(getAllocationBreakdownMock).toHaveBeenCalledTimes(1)
  })

  it('refetches_when_displayCurrency_changes', async () => {
    getAllocationBreakdownMock.mockResolvedValue(BREAKDOWN)

    const { rerender } = renderBreakdownHook('GBP')
    await waitFor(() => expect(getAllocationBreakdownMock).toHaveBeenCalledTimes(1))

    rerender({ currency: 'BRL', filter: 'ALL' })

    await waitFor(() => expect(getAllocationBreakdownMock).toHaveBeenCalledTimes(2))
    expect(getAllocationBreakdownMock).toHaveBeenLastCalledWith('BRL', undefined)
  })

  it('refetches_when_brokerCurrencyFilter_changes', async () => {
    getAllocationBreakdownMock.mockResolvedValue(BREAKDOWN)

    const { rerender } = renderBreakdownHook('GBP', 'ALL')
    await waitFor(() => expect(getAllocationBreakdownMock).toHaveBeenCalledTimes(1))

    rerender({ currency: 'GBP', filter: 'BRL' })

    await waitFor(() => expect(getAllocationBreakdownMock).toHaveBeenCalledTimes(2))
    expect(getAllocationBreakdownMock).toHaveBeenLastCalledWith('GBP', 'BRL')
  })

  it('passes_no_brokerCurrency_param_when_the_filter_is_ALL', async () => {
    getAllocationBreakdownMock.mockResolvedValue(BREAKDOWN)

    renderBreakdownHook('GBP', 'ALL')

    await waitFor(() => expect(getAllocationBreakdownMock).toHaveBeenCalledWith('GBP', undefined))
  })
})
