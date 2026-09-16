import { renderHook, waitFor } from '@testing-library/react'
import { act } from 'react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { FinancialApiClient } from '../../api/financialApiClient'
import type { PortfolioDashboardDto } from '../../api/types'
import { useDashboardSummary } from '../useDashboardSummary'

const { getDashboardMock } = vi.hoisted(() => ({
  getDashboardMock: vi.fn<FinancialApiClient['getDashboard']>(),
}))

vi.mock('../../api/financialApiClient', () => ({
  apiClient: {
    getDashboard: getDashboardMock,
  } as Partial<FinancialApiClient>,
}))

const SUMMARY: PortfolioDashboardDto = {
  marketValue: 18000,
  invested: 12220.5,
  unrealisedGainLoss: 5779.5,
  realisedGainLoss: 1200,
  incomeYtd: 340.25,
  incomeLifetime: 2840.75,
  grossXirr: 0.1234,
  netXirr: 0.0987,
  isPartial: false,
  unvaluedHoldingCount: 0,
  reportingCurrency: 'GBP',
  isReportingCurrencyEnabled: false,
  isReportingCurrencyPartial: false,
  isReportingCurrencyUnavailable: false,
  convertedMarketValue: null,
  convertedInvested: null,
  convertedUnrealisedGainLoss: null,
  convertedRealisedGainLoss: null,
  convertedIncomeYtd: null,
  convertedIncomeLifetime: null,
  convertedGrossXirr: null,
  convertedNetXirr: null,
}

describe('useDashboardSummary', () => {
  beforeEach(() => {
    getDashboardMock.mockReset()
  })

  it('fetches_the_dashboard_once_on_mount', async () => {
    getDashboardMock.mockResolvedValue(SUMMARY)

    const { result } = renderHook(() => useDashboardSummary())

    await waitFor(() => expect(result.current.summary).toEqual(SUMMARY))
    expect(getDashboardMock).toHaveBeenCalledTimes(1)
    expect(result.current.isLoading).toBe(false)
    expect(result.current.error).toBeNull()
  })

  it('reports_loading_until_the_request_resolves', async () => {
    getDashboardMock.mockResolvedValue(SUMMARY)

    const { result } = renderHook(() => useDashboardSummary())

    await waitFor(() => expect(result.current.isLoading).toBe(true))
    await waitFor(() => expect(result.current.isLoading).toBe(false))
  })

  it('exposes_the_failure_message_and_no_summary_when_the_request_rejects', async () => {
    getDashboardMock.mockRejectedValue(new Error('Service unavailable'))

    const { result } = renderHook(() => useDashboardSummary())

    await waitFor(() => expect(result.current.error).toBe('Service unavailable'))
    expect(result.current.summary).toBeNull()
    expect(result.current.isLoading).toBe(false)
  })

  it('falls_back_to_a_dashboard_specific_message_for_a_non_error_rejection', async () => {
    getDashboardMock.mockRejectedValue('boom')

    const { result } = renderHook(() => useDashboardSummary())

    await waitFor(() => expect(result.current.error).toBe('Unable to load dashboard summary'))
  })

  it('retry_re_issues_the_request', async () => {
    getDashboardMock.mockRejectedValueOnce(new Error('Service unavailable')).mockResolvedValue(SUMMARY)

    const { result } = renderHook(() => useDashboardSummary())
    await waitFor(() => expect(result.current.error).toBe('Service unavailable'))

    act(() => result.current.retry())

    await waitFor(() => expect(result.current.summary).toEqual(SUMMARY))
    expect(getDashboardMock).toHaveBeenCalledTimes(2)
    expect(result.current.error).toBeNull()
  })
})
