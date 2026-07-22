import { act, renderHook, waitFor } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { FinancialApiClient } from '../api/financialApiClient'
import type { CategoryYearlyTotalDto, InvestmentDiffsYearlyDto } from '../api/types'
import { useYearlySummary } from './useYearlySummary'

const CURRENT_YEAR = new Date().getFullYear()

const getCategoryTotalsForYearMock = vi.fn<FinancialApiClient['getCategoryTotalsForYear']>()
const getInvestmentDiffsForYearMock = vi.fn<FinancialApiClient['getInvestmentDiffsForYear']>()

vi.mock('../api/financialApiClient', () => ({
  createFinancialApiClient: (): Partial<FinancialApiClient> => ({
    getCategoryTotalsForYear: getCategoryTotalsForYearMock,
    getInvestmentDiffsForYear: getInvestmentDiffsForYearMock,
  }),
}))

const CATEGORY_TOTALS: CategoryYearlyTotalDto[] = [
  { category: 'Mercado', monthlyTotals: new Array(12).fill(100), yearlyTotal: 1200 },
]

const INVESTMENT_DIFFS: InvestmentDiffsYearlyDto = {
  accounts: [
    {
      account: 'ChaseSave',
      isLiability: false,
      monthlyValues: new Array(12).fill(1000),
      monthlyDiffs: new Array(11).fill(0),
    },
  ],
  netPosition: {
    monthlyValues: new Array(12).fill(1000),
    monthlyDiffs: new Array(11).fill(0),
    fullYearNetChange: 0,
  },
}

describe('useYearlySummary', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    getCategoryTotalsForYearMock.mockResolvedValue(CATEGORY_TOTALS)
    getInvestmentDiffsForYearMock.mockResolvedValue(INVESTMENT_DIFFS)
  })

  it('fetches category totals and investment diffs for the current year on mount', async () => {
    const { result } = renderHook(() => useYearlySummary())

    expect(result.current.isLoading).toBe(true)
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    expect(getCategoryTotalsForYearMock).toHaveBeenCalledWith(CURRENT_YEAR)
    expect(getInvestmentDiffsForYearMock).toHaveBeenCalledWith(CURRENT_YEAR)
    expect(result.current.year).toBe(CURRENT_YEAR)
    expect(result.current.categoryTotals).toEqual(CATEGORY_TOTALS)
    expect(result.current.investmentDiffs).toEqual(INVESTMENT_DIFFS)
  })

  it('re-fetches for a new year when the year changes', async () => {
    const { result } = renderHook(() => useYearlySummary())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.setYear(2020))

    await waitFor(() => expect(getCategoryTotalsForYearMock).toHaveBeenCalledWith(2020))
    await waitFor(() => expect(getInvestmentDiffsForYearMock).toHaveBeenCalledWith(2020))
  })

  it('surfaces a fetch error without crashing', async () => {
    getCategoryTotalsForYearMock.mockRejectedValue(new Error('Network down'))
    const { result } = renderHook(() => useYearlySummary())

    await waitFor(() => expect(result.current.error).toBe('Network down'))
  })

  it('re-fetches on retry', async () => {
    const { result } = renderHook(() => useYearlySummary())
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    act(() => result.current.retry())

    await waitFor(() => expect(getCategoryTotalsForYearMock).toHaveBeenCalledTimes(2))
  })
})
