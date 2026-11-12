import { act, renderHook, waitFor } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { FinancialApiClient } from '../../api/financialApiClient'
import type { AccountGroupSummaryDto, CategoryTotalDto } from '../../api/types'
import { useAccountsSummary } from '../useAccountsSummary'

const {
  getCategoryTotalsByMonthMock,
  getDefaultGroupSummaryMock,
  getTitheSummaryByMonthMock,
} = vi.hoisted(() => ({
  getCategoryTotalsByMonthMock: vi.fn<FinancialApiClient['getCategoryTotalsByMonth']>(),
  getDefaultGroupSummaryMock: vi.fn<FinancialApiClient['getDefaultGroupSummary']>(),
  getTitheSummaryByMonthMock: vi.fn<FinancialApiClient['getTitheSummaryByMonth']>(),
}))

vi.mock('../../api/financialApiClient', () => ({
  apiClient: {
    getCategoryTotalsByMonth: getCategoryTotalsByMonthMock,
    getDefaultGroupSummary: getDefaultGroupSummaryMock,
    getTitheSummaryByMonth: getTitheSummaryByMonthMock,
  } as Partial<FinancialApiClient>,
}))

const CATEGORY_TOTALS: CategoryTotalDto[] = [{ category: 'Mercado', totalValue: 42.5 }]

const SUMMARY: AccountGroupSummaryDto = {
  year: 2026,
  month: 7,
  members: [],
  totals: [{ currency: 'GBP', balance: 1825.32, roundUpTotal: 0.1, cardOutstandingTotal: 64.1 }],
  categoryTotalsSum: 1234.56,
  incomeTotals: [{ incomeSourceId: '2', name: 'Ariana', netValue: 720, grossValue: 820 }],
  totalIncoming: 720,
}

async function renderLoaded(year = 2026, month = 7) {
  const rendered = renderHook(() => useAccountsSummary(year, month))
  await waitFor(() => expect(rendered.result.current.isLoading).toBe(false))
  return rendered
}

describe('useAccountsSummary', () => {
  beforeEach(() => {
    getCategoryTotalsByMonthMock.mockReset().mockResolvedValue(CATEGORY_TOTALS)
    getDefaultGroupSummaryMock.mockReset().mockResolvedValue(SUMMARY)
    getTitheSummaryByMonthMock.mockReset()
  })

  it('[AC P60-F13-consolidated-navigation-and-retirement-02] makes only the two overview reads for the given month', async () => {
    const { result } = renderHook(() => useAccountsSummary(2026, 7))

    expect(result.current.isLoading).toBe(true)
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    expect(getCategoryTotalsByMonthMock).toHaveBeenCalledExactlyOnceWith(2026, 7)
    expect(getDefaultGroupSummaryMock).toHaveBeenCalledExactlyOnceWith(2026, 7)
    expect(result.current.categoryTotals).toEqual(CATEGORY_TOTALS)
  })

  it('[AC P61-F03-tithe-tab-and-history-05] makes no tithe request and exposes no tithe data', async () => {
    const { result } = await renderLoaded()

    expect(getTitheSummaryByMonthMock).not.toHaveBeenCalled()
    expect(result.current).not.toHaveProperty('titheSummary')
    expect(result.current).not.toHaveProperty('updateCarryForwardInclusion')
  })

  it('exposes the load error and reloads on retry', async () => {
    getDefaultGroupSummaryMock.mockRejectedValueOnce(new Error('Network down'))
    const { result } = await renderLoaded()
    expect(result.current.error).not.toBeNull()

    act(() => result.current.retry())

    await waitFor(() => expect(result.current.error).toBeNull())
    await waitFor(() => expect(result.current.isLoading).toBe(false))
    expect(getDefaultGroupSummaryMock).toHaveBeenCalledTimes(2)
    expect(result.current.totalIncoming).toBe(720)
  })

  it('[AC P60-F04-default-account-group-and-summary-query-06] reads the summary figures from one summary request without summing in the browser', async () => {
    const { result } = await renderLoaded()

    expect(getDefaultGroupSummaryMock).toHaveBeenCalledOnce()
    expect(result.current.categoryTotalsSum).toBe(1234.56)
    expect(result.current.incomeTotals).toEqual([{ incomeSourceId: '2', name: 'Ariana', netValue: 720, grossValue: 820 }])
    expect(result.current.totalIncoming).toBe(720)
  })

  it('[AC P61-F01-accounts-overview-tabs-and-monthly-fold-03] exposes no bank or card data', async () => {
    const { result } = await renderLoaded()

    expect(result.current).not.toHaveProperty('bankTotals')
    expect(result.current).not.toHaveProperty('cardMembers')
    expect(result.current).not.toHaveProperty('currencyTotals')
  })

  it('[AC P61-F01-accounts-overview-tabs-and-monthly-fold-03] gives the Result as Total incoming minus the Category totals sum', async () => {
    const { result } = await renderLoaded()

    expect(result.current.result).toEqual({ value: -514.56, label: 'Deficit' })
  })

  it('exposes empty summary figures and a Balanced result before the summary has loaded', () => {
    getDefaultGroupSummaryMock.mockReturnValue(new Promise(() => {}))
    const { result } = renderHook(() => useAccountsSummary(2026, 7))

    expect(result.current.incomeTotals).toEqual([])
    expect(result.current.categoryTotalsSum).toBe(0)
    expect(result.current.totalIncoming).toBe(0)
    expect(result.current.result).toEqual({ value: 0, label: 'Balanced' })
  })

  it('re-fetches when given a new month', async () => {
    const { rerender } = renderHook(({ month }) => useAccountsSummary(2026, month), { initialProps: { month: 7 } })
    await waitFor(() => expect(getDefaultGroupSummaryMock).toHaveBeenCalledWith(2026, 7))

    rerender({ month: 8 })

    await waitFor(() => expect(getDefaultGroupSummaryMock).toHaveBeenCalledWith(2026, 8))
    expect(getCategoryTotalsByMonthMock).toHaveBeenCalledWith(2026, 8)
  })

})
