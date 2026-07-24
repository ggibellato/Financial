import { act, renderHook, waitFor } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import type { FinancialApiClient } from '../api/financialApiClient'
import type { CategoryYearlyTotalDto, IncomeYearlySummaryDto, InvestmentDiffsYearlyDto } from '../api/types'
import { useYearlySummary } from './useYearlySummary'

const CURRENT_YEAR = new Date().getFullYear()

const getCategoryTotalsForYearMock = vi.fn<FinancialApiClient['getCategoryTotalsForYear']>()
const getInvestmentDiffsForYearMock = vi.fn<FinancialApiClient['getInvestmentDiffsForYear']>()
const getIncomeSummaryForYearMock = vi.fn<FinancialApiClient['getIncomeSummaryForYear']>()

vi.mock('../api/financialApiClient', () => ({
  createFinancialApiClient: (): Partial<FinancialApiClient> => ({
    getCategoryTotalsForYear: getCategoryTotalsForYearMock,
    getInvestmentDiffsForYear: getInvestmentDiffsForYearMock,
    getIncomeSummaryForYear: getIncomeSummaryForYearMock,
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

const INCOME_SUMMARY: IncomeYearlySummaryDto = {
  salaryMonthly: new Array(12).fill(3200),
  salaryYearlyTotal: 38400,
  salaryAfterTaxesMonthly: new Array(12).fill(2450),
  salaryAfterTaxesYearlyTotal: 29400,
  taxDifferenceMonthly: new Array(12).fill(750),
  taxDifferenceYearlyTotal: 9000,
  dividendoJurosMonthly: new Array(12).fill(15.5),
  dividendoJurosYearlyTotal: 186,
}

describe('useYearlySummary', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    getCategoryTotalsForYearMock.mockResolvedValue(CATEGORY_TOTALS)
    getInvestmentDiffsForYearMock.mockResolvedValue(INVESTMENT_DIFFS)
    getIncomeSummaryForYearMock.mockResolvedValue(INCOME_SUMMARY)
  })

  it('fetches category totals, investment diffs, and income summary for the current year on mount', async () => {
    const { result } = renderHook(() => useYearlySummary())

    expect(result.current.isLoading).toBe(true)
    await waitFor(() => expect(result.current.isLoading).toBe(false))

    expect(getCategoryTotalsForYearMock).toHaveBeenCalledWith(CURRENT_YEAR)
    expect(getInvestmentDiffsForYearMock).toHaveBeenCalledWith(CURRENT_YEAR)
    expect(getIncomeSummaryForYearMock).toHaveBeenCalledWith(CURRENT_YEAR)
    expect(result.current.year).toBe(CURRENT_YEAR)
    expect(result.current.categoryTotals).toEqual(CATEGORY_TOTALS)
    expect(result.current.investmentDiffs).toEqual(INVESTMENT_DIFFS)
    expect(result.current.incomeSummary).toEqual(INCOME_SUMMARY)
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
